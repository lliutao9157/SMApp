using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMApp
{
    public interface ISMAppCreater
    {
        void CreateApp(Smserver app);
        void DestoryApp(Smserver app);
    }
}
