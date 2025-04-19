﻿using Azure.Core.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary
{
    public class Tracker : IEntityWithId
    {
        public Tracker(string url)
        {
            Url = url;
        }

        public int Id { get; set; }
        public string Url { get; set; }
    }
}
