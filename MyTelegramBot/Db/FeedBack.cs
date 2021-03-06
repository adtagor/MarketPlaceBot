﻿using System;
using System.Collections.Generic;

namespace MyTelegramBot
{
    public partial class FeedBack
    {
        public FeedBack()
        {

        }

        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime? DateAdd { get; set; }
        public int? OrderId { get; set; }
        public int? RaitingValue { get; set; }
        public int? ProductId { get; set; }
        public bool Enable { get; set; }

        public Orders Order { get; set; }
        public Product Product { get; set; }

    }
}
