using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrpcSpaceServer.Services.Interfaces
{
    public interface IVibe2020DataService
    {
        public void Initialize();

        public List<double[]> GetData();
    }
}
