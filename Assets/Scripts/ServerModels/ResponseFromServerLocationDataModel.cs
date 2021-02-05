using System;
using System.Collections.Generic;
using UnityModels;

namespace ServerModels
{
    [Serializable]
    public class ResponseFromServerLocationDataModel
    {
        public bool success;
        public List<GeoPoiObjectModel> poiObjectModels;
        public List<Geo3dObjectModel> geo3dObjectModels;
        public List<GeoAudioObjectModel> geoAudioObjectModels;

        public override string ToString()
        {
            return $"Response success: {success} \n {poiObjectModels.ToString()}" +
                   $" \n {geoAudioObjectModels} \n {geo3dObjectModels}";
        }
    }
}