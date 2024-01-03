namespace NeoCortexApi.Tools
{
    public class ImageWithLine
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int LineThicknessInPercent { get; set; }
        public int LineLengthInPercent { get; set; }
        public int LineXAxisPositionInPercent { get; set; }
        public int LineYAxisPositionInPercent { get; set; }
        public bool useJitter { get; set; }
    }
}
