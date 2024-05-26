namespace NeoCortexApi.SimilarityExperiment.Configuration
{
    public class HomeostaticPlasticityControllerConfiguration
    {
        public int MinCycles { get; set; }
        
        public int MaxCycles { get; set; }
        
        public int NumOfCyclesToWaitOnChange { get; set; }
    }
}