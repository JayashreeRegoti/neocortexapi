using Microsoft.Extensions.Logging;

namespace NeoCortexApi.KnnSample
{
    public class HomeostaticPlasticityControllerConfiguration
    {
        public int MinCycles { get; set; }
        
        public int MaxCycles { get; set; }
        
        public int NumOfCyclesToWaitOnChange { get; set; }
    }
}
