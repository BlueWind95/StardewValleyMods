using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TehPers.Stardew.SCCL.Items;

namespace TehPers.Stardew.SCCL.Items {
    public interface ISavable {
        ItemTemplate Template { get; set; }
        Dictionary<string, object> Data { get; set; }
    }
}
