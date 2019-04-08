using System;
using System.Collections.Generic;
using System.Text;

namespace Beste.Aws.Databases.Connector
{
    public partial class AwsConfig : Beste.Xml.Xml
    {
        public string RegionEndpoint = "";

        public Amazon.RegionEndpoint GetRegionEndpoint()
        {
            if (RegionEndpoint == "APNortheast1")
                return Amazon.RegionEndpoint.APNortheast1;
            else if (RegionEndpoint == "APNortheast2")
                return Amazon.RegionEndpoint.APNortheast2;
            else if (RegionEndpoint == "APNortheast3")
                return Amazon.RegionEndpoint.APNortheast3;
            else if (RegionEndpoint == "APSouth1")
                return Amazon.RegionEndpoint.APSouth1;
            else if (RegionEndpoint == "APSoutheast1")
                return Amazon.RegionEndpoint.APSoutheast1;
            else if (RegionEndpoint == "APSoutheast2")
                return Amazon.RegionEndpoint.APSoutheast2;
            else if (RegionEndpoint == "CACentral1")
                return Amazon.RegionEndpoint.CACentral1;
            else if (RegionEndpoint == "CNNorth1")
                return Amazon.RegionEndpoint.CNNorth1;
            else if (RegionEndpoint == "CNNorthWest1")
                return Amazon.RegionEndpoint.CNNorthWest1;
            else if (RegionEndpoint == "EUCentral1")
                return Amazon.RegionEndpoint.EUCentral1;
            else if (RegionEndpoint == "EUNorth1")
                return Amazon.RegionEndpoint.EUNorth1;
            else if (RegionEndpoint == "EUWest1")
                return Amazon.RegionEndpoint.EUWest1;
            else if (RegionEndpoint == "EUWest2")
                return Amazon.RegionEndpoint.EUWest2;
            else if (RegionEndpoint == "EUWest3")
                return Amazon.RegionEndpoint.EUWest3;
            else if (RegionEndpoint == "SAEast1")
                return Amazon.RegionEndpoint.SAEast1;
            else if (RegionEndpoint == "USEast1")
                return Amazon.RegionEndpoint.USEast1;
            else if (RegionEndpoint == "USEast2")
                return Amazon.RegionEndpoint.USEast2;
            else if (RegionEndpoint == "USGovCloudEast1")
                return Amazon.RegionEndpoint.USGovCloudEast1;
            else if (RegionEndpoint == "USGovCloudWest1")
                return Amazon.RegionEndpoint.USGovCloudWest1;
            else if (RegionEndpoint == "USWest1")
                return Amazon.RegionEndpoint.USWest1;
            else if (RegionEndpoint == "USWest2")
                return Amazon.RegionEndpoint.USWest2;
            else
                return Amazon.RegionEndpoint.USEast2;
        }
    }
}
