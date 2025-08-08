namespace FrameByFrame.src.Engine
{
    public enum LayerType
    {
        Layer1 = 1,
        Layer2 = 2, 
        Layer3 = 3
    }
    
    public static class LayerExtensions
    {
        public static string ToLegacyString(this LayerType layer)
        {
            return layer switch
            {
                LayerType.Layer1 => UIConstants.LAYER_1,
                LayerType.Layer2 => UIConstants.LAYER_2,
                LayerType.Layer3 => UIConstants.LAYER_3,
                _ => UIConstants.LAYER_1
            };
        }
        
        public static LayerType FromLegacyString(string layerName)
        {
            return layerName switch
            {
                UIConstants.LAYER_1 => LayerType.Layer1,
                UIConstants.LAYER_2 => LayerType.Layer2,
                UIConstants.LAYER_3 => LayerType.Layer3,
                _ => LayerType.Layer1
            };
        }
        
        public static int GetDrawOrder(this LayerType layer)
        {
            return (int)layer; // Layer3 (3) draws over Layer2 (2) over Layer1 (1)
        }
    }
}