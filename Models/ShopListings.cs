﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SilvercityEtsyService.Models
{
    public class ShopListings
    {
        public int? count { get; set; }
        public List<Listing> results { get; set; } = new List<Listing>();
    }
}
