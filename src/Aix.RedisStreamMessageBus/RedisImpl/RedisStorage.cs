using Aix.RedisStreamMessageBus.Foundation;
using Aix.RedisStreamMessageBus.Model;
using Aix.RedisStreamMessageBus.Utils;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aix.RedisStreamMessageBus.RedisImpl
{
    /// <summary>
    /// 
    /// </summary>
    public class RedisStorage
    {
        private IServiceProvider _serviceProvider;
        private ConnectionMultiplexer _redis = null;
        private IDatabase _database;
        private RedisMessageBusOptions _options;
        private readonly RedisSubscription _delayJobChannelSubscription;

        public RedisStorage(IServiceProvider serviceProvider, ConnectionMultiplexer redis, RedisMessageBusOptions options)
        {
            _serviceProvider = serviceProvider;
            this._redis = redis;
            this._options = options;
            _database = redis.GetDatabase();

            _delayJobChannelSubscription = new RedisSubscription(_serviceProvider, _redis.GetSubscriber(), Helper.GetDelayChannel(_options));
        }

        #region Stream
        public async Task<string> StreamAdd(JobData jobData)
        {
            var nameValues = jobData.ToNameValueEntries();
            var streamName = jobData.Topic;
            var resust = await _database.StreamAddAsync(streamName, nameValues, messageId: null, maxLength: _options.StreamMaxLength, useApproximateMaxLength: true);
            return resust;
        }

        public Task StreamAddTransaction(JobData jobData, ITransaction transaction)
        {
            var nameValues = jobData.ToNameValueEntries();
            var streamName = jobData.Topic;
            return transaction.StreamAddAsync(streamName, nameValues, messageId: null, maxLength: _options.StreamMaxLength, useApproximateMaxLength: true);
        }

        public async Task CreateConsumerGroupIfNotExist(string streamName, string groupName, RedisValue? position = null)
        {
            if (!await ExistGroup(streamName, groupName))
            {
                //$或id,指创建消费组的这一刻，$:只能读取此刻以后的新消息 , id:可以读取该id以后的消息  只是针对创建时的情况，一旦建好 以后都一样了  以后的消息都会接收，就是创建时会设置lastid（具体id还是最新的id）
                var createGroup = await _database.StreamCreateConsumerGroupAsync(streamName, groupName, position, true);//$(可以读取该组创建以后的消息)或者 id(可以读该id以后的消息)或者  0-0
            }
        }

        public async Task<bool> ExsitsStream(string streamName)
        {
            try
            {
                await _database.StreamInfoAsync(streamName);
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().IndexOf("no such key") >= 0)
                {
                    return false;
                }
                throw;
            }

            return true;
        }

        public async Task<bool> ExistGroup(string streamName, string groupName)
        {
            if (!(await ExsitsStream(streamName))) return false;

            var groupInfos = await _database.StreamGroupInfoAsync(streamName);
            if (groupInfos != null)
            {
                return groupInfos.ToList().Exists(x => x.Name == groupName);
            }

            return false;
        }

        #endregion

        #region 延时任务

        /// <summary>
        /// 延时任务
        /// </summary>
        /// <param name="jobData"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        public async Task<bool> EnqueueDealy(JobData jobData, TimeSpan delay)
        {
            var values = jobData.ToHashEntries();
            var hashJobId = Helper.GetJobHashId(_options, jobData.JobId);

            var trans = _database.CreateTransaction();
#pragma warning disable CS4014
            trans.HashSetAsync(hashJobId, values.ToArray());
            trans.SortedSetAddAsync(Helper.GetDelayTopic(_options, jobData.JobId), jobData.JobId, DateUtils.GetTimeStamp(DateTime.Now.Add(delay))); //当前时间戳，
#pragma warning restore CS4014
            var result = await trans.ExecuteAsync();

            if (delay < TimeSpan.FromSeconds(_options.DelayTaskPreReadSecond))
            {
                await _database.PublishAsync(_delayJobChannelSubscription.Channel, jobData.JobId);
            }

            return result;
        }

        /// <summary>
        /// 查询到期的延迟任务
        /// </summary>
        /// <param name="delayTopicName"></param>
        /// <param name="timeStamp"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public Task<IDictionary<string, long>> GetTopDueDealyJobId(string delayTopicName, long timeStamp, int count)
        {
            var nowTimeStamp = timeStamp;
            var result = _database.SortedSetRangeByScoreWithScores(delayTopicName, double.NegativeInfinity, nowTimeStamp, Exclude.None, Order.Ascending, 0, count);
            IDictionary<string, long> dict = new Dictionary<string, long>();
            foreach (SortedSetEntry item in result)
            {
                dict.Add(item.Element, (long)item.Score);
            }
            return Task.FromResult(dict);
        }


        /// <summary>
        /// 到期的延迟任务
        /// </summary>
        /// <param name="delayTopicName"></param>
        /// <param name="jobData"></param>
        /// <returns></returns>
        public async Task<bool> DueDealyJobEnqueue(string delayTopicName, JobData jobData)
        {
            var jobId = jobData.JobId;

            var trans = _database.CreateTransaction();
#pragma warning disable CS4014
            StreamAddTransaction(jobData, trans);
            //trans.SortedSetRemoveAsync(Helper.GetDelaySortedSetName(_options), jobId);
            trans.SortedSetRemoveAsync(delayTopicName, jobId);
            trans.KeyDeleteAsync(Helper.GetJobHashId(_options, jobId));
#pragma warning restore CS4014
            var result = await trans.ExecuteAsync();
            return result;
        }

        /// <summary>
        /// 删除数据为空的 延迟任务数据  一般不会有这种情况的
        /// </summary>
        /// <param name="delayTopicName"></param>
        /// <param name="jobId"></param>
        /// <returns></returns>
        public async Task<bool> RemoveNullDealyJob(string delayTopicName, string jobId)
        {
            var trans = _database.CreateTransaction();
#pragma warning disable CS4014
            //trans.SortedSetRemoveAsync(Helper.GetDelaySortedSetName(_options), jobId);
            trans.SortedSetRemoveAsync(delayTopicName, jobId);
            trans.KeyDeleteAsync(Helper.GetJobHashId(_options, jobId));
#pragma warning restore CS4014
            var result = await trans.ExecuteAsync();

            return result;
        }


        #endregion

        public void WaitForDelayJob(TimeSpan timeSpan, CancellationToken cancellationToken = default(CancellationToken))
        {
            _delayJobChannelSubscription.WaitForJob(timeSpan, cancellationToken);
        }

        public async Task Lock(string key, TimeSpan span, Func<Task> action, Func<Task> concurrentCallback = null)
        {
            string token = Guid.NewGuid().ToString();
            if (LockTake(key, token, span))
            {
                try
                {
                    await action();
                }
                catch
                {
                    throw;
                }
                finally
                {
                    LockRelease(key, token);
                }
            }
            else
            {
                if (concurrentCallback != null) await concurrentCallback();
                else throw new Exception($"出现并发key:{key}");
            }
        }

        #region redis 基本操作
        public async Task<HashEntry[]> HashGetAll(string key)
        {
            var hashEntities = await _database.HashGetAllAsync(key);
            return hashEntities;
        }

        public Task<bool> KeyDelete(string key)
        {
            return _database.KeyDeleteAsync(key);
        }

        #endregion

        #region private

        /// <summary>
        /// 获取一个锁
        /// </summary>
        /// <param name="key"></param>
        /// <param name="token"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        private bool LockTake(string key, string token, TimeSpan expiry)
        {
            return _database.LockTake(key, token, expiry);
        }

        /// <summary>
        /// 释放一个锁(需要token匹配)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool LockRelease(string key, string token)
        {
            return _database.LockRelease(key, token);
        }

        #endregion
    }
}
