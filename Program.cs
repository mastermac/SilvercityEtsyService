using Newtonsoft.Json;
using RestSharp;
using SilvercityEtsyService.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using TinyOAuth1;

namespace SilvercityEtsyService
{
    class Program
    {
        static string getDataStart = "0\":";
        static int pageLimit = 100;
        private static int pollingTime = 1000 * 60 * 60 * 24; //Every 24 Hours
        public static int requestCounter = 0;
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Application @ "+DateTime.Now);
            //sampleUpdateAsync();
            while (true)
            {
                PollActiveListings();
                PollSoldOutListings();
                PollExpiredListings();
                PollInActiveListings();
                preventIdlingOut();
            }
        }
        static void preventIdlingOut()
        {
            int totalTime = 0;
            int idleTime = 1000 * 60 * 15;
            while (true)
            {
                totalTime += idleTime;
                if (totalTime >= pollingTime)
                    break;
                Thread.Sleep(idleTime);
                for (int i = 0; i < 10000; i++);
            }
        }
        static void PollActiveListings()
        {
            Console.WriteLine();
            Console.WriteLine("Active State Polling Started");
            int pageNo = 1;
            while (true)
            {
                var client = new RestClient();
                ShopListings shopListing = new ShopListings();
                client.BaseUrl = new System.Uri("https://openapi.etsy.com/v2/shops/maahira/listings/active?limit=" + pageLimit + "&page=" + pageNo++);
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", "OAuth " + OAuthSignatureGenerator.GetAuthorizationHeaderValue(client.BaseUrl, "","GET"));
                IRestResponse response = client.Execute(request);
                CheckRequestThrottleLimit();
                shopListing = JsonConvert.DeserializeObject<ShopListings>(response.Content);
                if (shopListing.count != null && shopListing.count > 0 && shopListing.results.Count > 0)
                {
                    foreach (Listing listingItem in shopListing.results)
                    {
                        if (listingItem.sku.Count == 1)
                        {
                            var client1 = new RestClient("https://www.silvercityonline.com/stock/src/scripts/getData.php?perPage=50&page=1&itemNo=" + listingItem.sku[0] + "&sdt=0000-00-00&edt=0000-00-00");
                            var request1 = new RestRequest(Method.GET);
                            IRestResponse response1 = client1.Execute(request1);
                            CheckRequestThrottleLimit();
                            if (response1.Content.Contains(getDataStart))
                            {
                                int startInd = response1.Content.IndexOf(getDataStart);
                                string substr = response1.Content.Substring(startInd + getDataStart.Length);
                                ItemData stockItem = new ItemData();
                                stockItem = JsonConvert.DeserializeObject<ItemData>(substr.Substring(0, substr.IndexOf("}") + 1));
                                if (listingItem.state == "edit" && int.Parse(stockItem.curStock) > 0)
                                {
                                    changeInventoryState(listingItem.listing_id, "active");
                                    updateInventory(listingItem.listing_id, stockItem.sellPrice, stockItem.curStock, stockItem.itemNo);
                                }
                                else if (int.Parse(stockItem.curStock) != listingItem.quantity || double.Parse(listingItem.price) != double.Parse(stockItem.sellPrice))
                                {
                                        if (int.Parse(stockItem.curStock) > 0)
                                            updateInventory(listingItem.listing_id, stockItem.sellPrice, stockItem.curStock, stockItem.itemNo);
                                        else if (listingItem.state != "edit")
                                            changeInventoryState(listingItem.listing_id, "inactive");
                                }
                            }
                        }
                    }
                    Console.WriteLine("Done for Page " + (pageNo - 1) + " @ " + DateTime.Now);
                }
                else
                {
                    pageNo = 1;
                    break;
                }
            }
            Console.WriteLine();
            Console.WriteLine("Active State Polling Done");
        }
        static void PollInActiveListings()
        {
            Console.WriteLine();
            Console.WriteLine("InActive State Polling Started");
            int pageNo = 1;
            while (true)
            {
                var client = new RestClient();
                ShopListings shopListing = new ShopListings();
                client.BaseUrl = new System.Uri("https://openapi.etsy.com/v2/shops/maahira/listings/inactive?limit=" + pageLimit + "&page=" + pageNo++);
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", "OAuth "+ OAuthSignatureGenerator.GetAuthorizationHeaderValue(client.BaseUrl, "","GET"));
                IRestResponse response = client.Execute(request);
                CheckRequestThrottleLimit();
                shopListing = JsonConvert.DeserializeObject<ShopListings>(response.Content);
                if (shopListing.count != null && shopListing.count > 0 && shopListing.results.Count > 0)
                {
                    foreach (Listing listingItem in shopListing.results)
                    {
                        if (listingItem.sku.Count == 1)
                        {
                            var client1 = new RestClient("https://www.silvercityonline.com/stock/src/scripts/getData.php?perPage=50&page=1&itemNo=" + listingItem.sku[0] + "&sdt=0000-00-00&edt=0000-00-00");
                            var request1 = new RestRequest(Method.GET);
                            IRestResponse response1 = client1.Execute(request1);
                            CheckRequestThrottleLimit();
                            if (response1.Content.Contains(getDataStart))
                            {
                                int startInd = response1.Content.IndexOf(getDataStart);
                                string substr = response1.Content.Substring(startInd + getDataStart.Length);
                                ItemData stockItem = new ItemData();
                                stockItem = JsonConvert.DeserializeObject<ItemData>(substr.Substring(0, substr.IndexOf("}") + 1));
                                if (int.Parse(stockItem.curStock) > 0)
                                {
                                    changeInventoryState(listingItem.listing_id, "active");
                                    updateInventory(listingItem.listing_id, stockItem.sellPrice, stockItem.curStock, stockItem.itemNo);
                                }
                            }
                        }
                    }
                    Console.WriteLine("Done for Page " + (pageNo - 1) + " @ " + DateTime.Now);
                }
                else
                {
                    pageNo = 1;
                    break;
                }
            }
            Console.WriteLine();
            Console.WriteLine("InActive State Polling Done");
        }
        static void PollExpiredListings()
        {
            Console.WriteLine();
            Console.WriteLine("Expired State Polling Started");
            int pageNo = 1;
            while (true)
            {
                var client = new RestClient();
                ShopListings shopListing = new ShopListings();
                client.BaseUrl = new System.Uri("https://openapi.etsy.com/v2/shops/maahira/listings/expired?limit=" + pageLimit + "&page=" + pageNo++);
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", "OAuth " + OAuthSignatureGenerator.GetAuthorizationHeaderValue(client.BaseUrl, "", "GET"));
                IRestResponse response = client.Execute(request);
                CheckRequestThrottleLimit();
                shopListing = JsonConvert.DeserializeObject<ShopListings>(response.Content);
                if (shopListing.count != null && shopListing.count > 0 && shopListing.results.Count > 0)
                {
                    foreach (Listing listingItem in shopListing.results)
                    {
                        if (listingItem.sku.Count == 1)
                        {
                            var client1 = new RestClient("https://www.silvercityonline.com/stock/src/scripts/getData.php?perPage=50&page=1&itemNo=" + listingItem.sku[0] + "&sdt=0000-00-00&edt=0000-00-00");
                            var request1 = new RestRequest(Method.GET);
                            IRestResponse response1 = client1.Execute(request1);
                            CheckRequestThrottleLimit();
                            if (response1.Content.Contains(getDataStart))
                            {
                                int startInd = response1.Content.IndexOf(getDataStart);
                                string substr = response1.Content.Substring(startInd + getDataStart.Length);
                                ItemData stockItem = new ItemData();
                                stockItem = JsonConvert.DeserializeObject<ItemData>(substr.Substring(0, substr.IndexOf("}") + 1));
                                if (int.Parse(stockItem.curStock) > 0)
                                {
                                    changeInventoryState(listingItem.listing_id, "active");
                                    updateInventory(listingItem.listing_id, stockItem.sellPrice, stockItem.curStock, stockItem.itemNo);
                                }
                            }
                        }
                    }
                    Console.WriteLine("Done for Page " + (pageNo - 1) + " @ " + DateTime.Now);
                }
                else
                {
                    pageNo = 1;
                    break;
                }
            }
            Console.WriteLine();
            Console.WriteLine("Expired State Polling Done");
        }
        static void PollSoldOutListings()
        {
            Console.WriteLine();
            Console.WriteLine("SoldOut State Polling Started");
            int pageNo = 1;
            while (true)
            {
                var client = new RestClient();
                Transactions transactions = new Transactions();
                client.BaseUrl = new System.Uri("https://openapi.etsy.com/v2/shops/maahira/transactions?includes=Listing&limit=" + pageLimit + "&page=" + pageNo++);
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", "OAuth " + OAuthSignatureGenerator.GetAuthorizationHeaderValue(client.BaseUrl, "", "GET"));
                IRestResponse response = client.Execute(request);
                CheckRequestThrottleLimit();
                transactions = JsonConvert.DeserializeObject<Transactions>(response.Content);
                if (transactions.count != null && transactions.count > 0 && transactions.results.Count > 0)
                {
                    foreach (TransactionDetails transaction in transactions.results)
                    {
                        if (transaction.Listing.sku.Count == 1 && !String.IsNullOrEmpty(transaction.Listing.state) && transaction.Listing.state=="sold_out")
                        {
                            var client1 = new RestClient("https://www.silvercityonline.com/stock/src/scripts/getData.php?perPage=50&page=1&itemNo=" + transaction.Listing.sku[0] + "&sdt=0000-00-00&edt=0000-00-00");
                            var request1 = new RestRequest(Method.GET);
                            IRestResponse response1 = client1.Execute(request1);
                            CheckRequestThrottleLimit();
                            if (response1.Content.Contains(getDataStart))
                            {
                                int startInd = response1.Content.IndexOf(getDataStart);
                                string substr = response1.Content.Substring(startInd + getDataStart.Length);
                                ItemData stockItem = new ItemData();
                                stockItem = JsonConvert.DeserializeObject<ItemData>(substr.Substring(0, substr.IndexOf("}") + 1));
                                if (int.Parse(stockItem.curStock) > 0)
                                {
                                    changeInventoryState(transaction.Listing.listing_id, "active");
                                    updateInventory(transaction.Listing.listing_id, stockItem.sellPrice, stockItem.curStock, stockItem.itemNo);
                                }
                            }
                        }
                    }
                    Console.WriteLine("Done for Page " + (pageNo - 1) + " @ " + DateTime.Now);
                }
                else
                {
                    pageNo = 1;
                    break;
                }
            }
            Console.WriteLine();
            Console.WriteLine("Sold Out State Polling Done");
        }
        static void CheckRequestThrottleLimit()
        {
            requestCounter++;
            if(requestCounter%4==0)
                Thread.Sleep(1000);
            requestCounter %= 4;
        }
        static void changeInventoryState(int listingId, string state)
        {
            var renewParam = "";
            if (state == "active")
                renewParam = "&renew=true";
            var client = new RestClient("https://openapi.etsy.com/v2/listings/"+listingId+"?state="+state+ renewParam);
            var request = new RestRequest(Method.PUT);
            request.AddHeader("Authorization", "OAuth "+ OAuthSignatureGenerator.GetAuthorizationHeaderValue(client.BaseUrl, ""));
            IRestResponse response = client.Execute(request);
            CheckRequestThrottleLimit();
            Console.WriteLine("Changing State to "+state+" for listing Id: " + listingId);
            //Console.WriteLine(response.Content);
        }
        static void updateInventory(int listingId, string sellPrice, string currentQty, string sku)
        {
            var client = new RestClient();
            client.BaseUrl = new Uri("https://openapi.etsy.com/v2/listings/" + listingId + "/inventory?api_key=3ptctueuc44gh9e3sny1oix5&write_missing_inventory=true");
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            CheckRequestThrottleLimit();
            try
            {
                var inventoryVariations = JsonConvert.DeserializeObject<GetInventory>(response.Content);
                if (inventoryVariations.results.products.Count == 1)
                {
                    List<UpdateInventory> updateInventoryList = new List<UpdateInventory>();
                    UpdateInventory updateInventory = new UpdateInventory();
                    updateInventory.product_id = inventoryVariations.results.products[0].product_id;
                    updateInventory.offerings.Add(new UpdateOffering());
                    updateInventory.offerings[0].offering_id = inventoryVariations.results.products[0].offerings[0].offering_id;
                    updateInventory.offerings[0].price = sellPrice;
                    updateInventory.offerings[0].quantity = int.Parse(currentQty);
                    updateInventory.sku = sku;
                    updateInventoryList.Add(updateInventory);

                    var client1 = new RestClient("https://openapi.etsy.com/v2/listings/" + listingId + "/inventory");
                    var request1 = new RestRequest(Method.PUT);

                    request1.AddHeader("Authorization", "OAuth " + OAuthSignatureGenerator.GetAuthorizationHeaderValue(client1.BaseUrl, JsonConvert.SerializeObject(updateInventoryList)));
                    request1.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                    request1.AddParameter("products", JsonConvert.SerializeObject(updateInventoryList));

                    IRestResponse response1 = client1.Execute(request1);
                    CheckRequestThrottleLimit();
                    Console.WriteLine("Stock Item is Updated with Listing ID: " + listingId);
                    //Console.WriteLine(response1.Content);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error for " + listingId + "-----" + e.StackTrace);
            }
        }

        static void sampleUpdateAsync(int listingId = 792035687, string sellPrice = "1350", string currentQty = "1")
        {
            var client = new RestClient();
            client.BaseUrl = new System.Uri("https://openapi.etsy.com/v2/listings/" + listingId + "/inventory?api_key=3ptctueuc44gh9e3sny1oix5&write_missing_inventory=true");
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            var inventoryVariations = JsonConvert.DeserializeObject<GetInventory>(response.Content);
            if (inventoryVariations.results.products.Count == 1)
            {
                List<UpdateInventory> updateInventoryList = new List<UpdateInventory>();
                UpdateInventory updateInventory = new UpdateInventory();
                updateInventory.product_id = inventoryVariations.results.products[0].product_id;
                updateInventory.offerings.Add(new UpdateOffering());
                updateInventory.offerings[0].offering_id = inventoryVariations.results.products[0].offerings[0].offering_id;
                updateInventory.offerings[0].price = sellPrice;
                updateInventory.offerings[0].quantity = int.Parse(currentQty);
                updateInventoryList.Add(updateInventory);

                var client1 = new RestClient("https://openapi.etsy.com/v2/listings/" + listingId + "/inventory");
                var request1 = new RestRequest(Method.PUT);

                request1.AddHeader("Authorization", "OAuth " + OAuthSignatureGenerator.GetAuthorizationHeaderValue(new Uri("https://openapi.etsy.com/v2/listings/" + listingId + "/inventory"), JsonConvert.SerializeObject(updateInventoryList)));
                request1.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request1.AddParameter("products", JsonConvert.SerializeObject(updateInventoryList));

                IRestResponse response1 = client1.Execute(request1);
                Console.WriteLine(response1.Content);
            }
        }
    }
}
