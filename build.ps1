$DOCKER_REGISTRY = "docker.io/jerkerolofsson"

cd src

echo "Building ${DOCKER_REGISTRY}/testbucket-ntp.."

cd TestBucket.Ntp
dotnet publish --os linux --arch x64 -p ContainerRepository=$DOCKER_REGISTRY/testbucket-ntp /t:PublishContainer
docker push ${DOCKER_REGISTRY}/testbucket-ntp
cd ..

cd ..

