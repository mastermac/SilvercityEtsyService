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

        static void Main(string[] args)
        {
            int pageNo = 1;
            Console.WriteLine("Hello World!");
            Console.WriteLine(DateTime.Now);
            //sampleUpdateAsync();
            var client = new RestClient();
            ShopListings shopListing = new ShopListings();
            while (true)
            {
                client.BaseUrl = new System.Uri("https://openapi.etsy.com/v2/shops/maahira/listings/active?api_key=3ptctueuc44gh9e3sny1oix5&limit=" + pageLimit + "&page=" + pageNo++);
                var request = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(request);
                shopListing = JsonConvert.DeserializeObject<ShopListings>(response.Content);
                if (shopListing.count != null && shopListing.count > 0 && shopListing.results.Count>0)
                {
                    foreach (Listing listingItem in shopListing.results)
                    {
                        if (listingItem.sku.Count > 0)
                        {

                            var client1 = new RestClient("https://www.silvercityonline.com/stock/src/scripts/getData.php?perPage=50&page=1&itemNo=" + listingItem.sku[0] + "&sdt=0000-00-00&edt=0000-00-00");
                            var request1 = new RestRequest(Method.GET);
                            IRestResponse response1 = client1.Execute(request1);
                            if (response1.Content.Contains(getDataStart))
                            {
                                int startInd = response1.Content.IndexOf(getDataStart);
                                string substr = response1.Content.Substring(startInd + getDataStart.Length);
                                ItemData stockItem = new ItemData();
                                stockItem = JsonConvert.DeserializeObject<ItemData>(substr.Substring(0, substr.IndexOf("}") + 1));
                                if (int.Parse(stockItem.curStock) != listingItem.quantity || double.Parse(listingItem.price) != double.Parse(stockItem.sellPrice))
                                {
                                    if (int.Parse(stockItem.curStock) > 0)
                                        updateInventory(listingItem.listing_id, stockItem.sellPrice, stockItem.curStock);
                                    else
                                        Console.WriteLine("Stock is empty for ItemNo: " + stockItem.itemNo + " with listing Id: " + listingItem.listing_id);
                                }
                            }
                        }
                    }
                    Console.WriteLine("Done for Page " + (pageNo - 1) + " @ " + DateTime.Now);
                }
                else
                {
                    pageNo = 1;
                    Thread.Sleep(pollingTime);
                }
            }
        }

        static void updateInventory(int listingId, string sellPrice, string currentQty)
        {
            var client = new RestClient();
            client.BaseUrl = new Uri("https://openapi.etsy.com/v2/listings/" + listingId + "/inventory?api_key=3ptctueuc44gh9e3sny1oix5&write_missing_inventory=true");
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
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
