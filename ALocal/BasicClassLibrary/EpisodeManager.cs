﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary
{
    public class EpisodeManager
    {
        public void Add(Episode episode)
        {
            using (var context = new EpisodeContext())
            {
                context.Episodes.Add(episode);
                context.SaveChanges();
            }
        }

        public Episode? FindById(int id)
        {
            using (var context = new EpisodeContext())
            {
                return context.Episodes
                    .SingleOrDefault(ep => ep.Id == id);
            }
        }

        public List<Episode> FindByEntryId(int entryId)
        {
            using (var context = new EpisodeContext())
            {
                return context.Episodes
                    .Where(ep => ep.EntryId == entryId)
                    .OrderBy(ep => ep.Id)
                    .ToList();
            }
        }
    }
}
