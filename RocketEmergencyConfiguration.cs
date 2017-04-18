using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rocket.API;
namespace NightFish.Emergency
{
    public class RocketEmergencyConfiguration : IRocketPluginConfiguration
    {
        public bool ShowCoordinates = false;

        public void LoadDefaults()
        {
            ShowCoordinates = false;
        }
    }
}
