using System;
using System.Collections.Generic;
using System.Text;

namespace MaxLib.Data.Config
{
    [Config("Base Config")]
    public abstract class ConfigBase
    {
        public abstract IEnumerable<IConfigValueBase> GetConfigs();
    }
}
