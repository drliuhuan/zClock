// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;

namespace zClock
{
    public sealed class PrompterSlide
    {
        public int index { get; set; }
        public string title { get; set; }
        public string notes { get; set; }
    }
    public sealed class PrompterState
    {
        public bool presenting { get; set; }
        public bool previewEnabled { get; set; }
        public string presentation { get; set; }
        public int current { get; set; }
        public int total { get; set; }
        public string timer { get; set; }
        public bool paused { get; set; }
        public string status { get; set; }
        public string deck { get; set; }
        public List<PrompterSlide> slides { get; set; }
        public static PrompterState Waiting(string status)
        {
            return new PrompterState { presentation = "zClock 提词器", status = status, timer = "", deck = "", slides = new List<PrompterSlide>() };
        }
    }
}
