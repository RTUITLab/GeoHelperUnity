using System;

namespace ServerModels
{
    // [Serializable]
    public interface IGeoObjectModel
    {
        string id { get; set; }
        string name { get; set; }
        string type { get; set; }
        LocationDataModel position { get; set; }
        // string ToString();
    }
}