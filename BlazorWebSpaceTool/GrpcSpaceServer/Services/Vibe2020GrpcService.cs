using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using BrctcSpace;
using System;
using GrpcSpaceServer.Services.Interfaces;
using System.Linq;
using BrctcSpaceLibrary;

namespace GrpcSpaceServer.Services
{
    public class Vibe2020GrpcService : Vibe.VibeBase
    {
        private readonly ILogger<Vibe2020GrpcService> _logger;
        private readonly IVibe2020DataService _dataService;

        public Vibe2020GrpcService(ILogger<Vibe2020GrpcService> logger, IVibe2020DataService dataService)
        {
            _logger = logger;
            _dataService = dataService;
        }

        public override Task<DeviceDataModel> GetSingleDeviceData(DeviceDataRequest request, ServerCallContext context)
        {
            _dataService.GetSingleReading();
            
            return Task.FromResult( _dataService.GetSingleReading() );
        }

        public override async Task StreamDeviceData(DeviceDataRequest request, IServerStreamWriter<DeviceDataModel> responseStream, ServerCallContext context)
        {
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    DeviceDataModel resultReply = _dataService.GetSingleReading(); 

                    if (context.CancellationToken.IsCancellationRequested)
                        context.CancellationToken.ThrowIfCancellationRequested();
                    await responseStream.WriteAsync(resultReply);
                }
            }
            catch(OperationCanceledException)
            {
                _logger.LogInformation("Stream cancelled by user request.");
            }
        }

        public override Task<DeviceDataArray> GetBulkDeviceData(DeviceDataRequest request, ServerCallContext context)
        {
            var response = new DeviceDataArray();
            response.Items.AddRange(_dataService.GetReadings(request.DataIterations));
            return Task.FromResult( response );
        }
    }
}
