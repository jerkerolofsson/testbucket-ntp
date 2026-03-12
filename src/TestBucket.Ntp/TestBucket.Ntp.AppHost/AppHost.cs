var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.TestBucket_Ntp>("testbucket-ntp");

builder.Build().Run();
