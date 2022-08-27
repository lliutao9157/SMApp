using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XX.PD.Interface;

namespace XX.PD
{
    public class Pdserver
    {
        private readonly static Lazy<Pdserver> AppSetInstance = new Lazy<Pdserver>(() => new Pdserver());
        public static Pdserver GetInstance()
        {
            return AppSetInstance.Value;
        }
        public ITest Test { get; set; } = AppSet.Instance.GetFromFac<ITest>();
    }
}
