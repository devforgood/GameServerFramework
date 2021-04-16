using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby.Models
{
    public partial class Character : DirtyUpdate, IBaseModel
    {
        public void Copy(Character other)
        {
            character_type = other.character_type;
            character_level = other.character_level;
            rank_level = other.rank_level;
            battle_score = other.battle_score;
            piece = other.piece;
        }

        public long GetUserNo() { return user_no; }
        public long GetValue() { return character_no; }
        public int GetKey() { return character_type; }

        public void SetUpdater(Func<Task> func)
        {
            updater = func;
        }

        public void SetDirty(bool isDirty)
        {
            IsDirty = isDirty;
        }
    }
}
