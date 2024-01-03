namespace NeoCortexApi.Tools
{
    public class ImageWithLine
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int LineThicknessInPercent { get; set; }
        public int LineLengthInPercent { get; set; }
        public int RowPositionInPercent { get; set; }
        public int ColumnPositionInPercent { get; set; }
        public bool useJitter { get; set; }
    }
}
