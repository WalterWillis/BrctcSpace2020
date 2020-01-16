using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using BrctcSpace;

namespace GrpcSpaceServer
{
    public class Vibe2020Service : Vibe.VibeBase
    {
        private readonly ILogger<Vibe2020Service> _logger;
        public Vibe2020Service(ILogger<Vibe2020Service> logger)
        {
            _logger = logger;
        }

        public override Task<ResultReply> GetResultSet(ResultRequest request, ServerCallContext context)
        {
            return Task.FromResult(new ResultReply
            {
                ResultStatus = (int)ResultStatus.Good,
                ResultSet = new ResultSet
                {
                    Accelerometer = new Accelerometer { X = 1, Y = 2, Z = 3 },
                    Gyroscope = new Gyroscope { BurstResults = new BurstResults { } }
                }
            });
        }
    }
}
