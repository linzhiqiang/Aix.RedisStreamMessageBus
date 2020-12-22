set -ex

cd $(dirname $0)/../

artifactsFolder="./artifacts"

if [ -d $artifactsFolder ]; then
  rm -R $artifactsFolder
fi

mkdir -p $artifactsFolder

dotnet restore ./Aix.RedisStreamMessageBus.sln
dotnet build ./Aix.RedisStreamMessageBus.sln -c Release


dotnet pack ./src/Aix.RedisStreamMessageBus/Aix.RedisStreamMessageBus.csproj -c Release -o $artifactsFolder
