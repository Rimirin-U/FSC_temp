﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary
{
    public class EntryRatingManager : Manager<EntryRating>
    {
        public EntryRatingManager() : base(new AppDbContext()) { }

        public static readonly Func<int, Func<EntryRating, bool>> ByEntryId = (entryId => (o => o.EntryId == entryId));
    }
}
