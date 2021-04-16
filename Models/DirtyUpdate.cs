using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lobby.Models
{
    public class DirtyUpdate : IAsyncDisposable
    {
        [NotMapped]
        [NonSerialized]
        public bool IsDirty = false;

        [NotMapped]
        [NonSerialized]
        public Func<Task> updater = null;

        public async ValueTask DisposeAsync()
        {
            if(IsDirty ==  true && updater != null)
            {
                await updater.Invoke();
                IsDirty = false;
            }
        }
    }
}
