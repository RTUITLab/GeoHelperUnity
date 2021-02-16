using System;

namespace ServerModels
{
    [Serializable]
    public class LocationDataModel 
    {
        public LocationDataModel(float lat, float lng)
        {
            this.lat = lat;
            this.lng = lng;
        }

        public float lat;
        public float lng;

        public override string ToString()
        {
            return base.ToString() + $" lat: {lat}, long: {lng}";
        }
    }
}