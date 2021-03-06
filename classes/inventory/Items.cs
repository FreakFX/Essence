﻿using Essence.classes.datahandles;
using GrandTheftMultiplayer.Server.API;
using GrandTheftMultiplayer.Server.Elements;
using GrandTheftMultiplayer.Shared;
using GrandTheftMultiplayer.Shared.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essence.classes.inventory
{
    public static class Items
    {
        // List of spawned Items
        private static List<OverworldItem> items = new List<OverworldItem>();
        public static List<OverworldItem> ActiveItems
        {
            get
            {
                return items;
            }
        }

        // List of all types of Items that a player can carry in his inventory
        public enum ItemTypes
        {
            //Food
            SANDWICH = 101,
            HAMBURGER,
            DONUT,
            PIZZA_SLICE,
            HOT_DOG,

            //Drinks
            SPRUNK = 201,
            PISSWASSER,
            ECOLA,

            //Drugs
            COCAINE = 301,
            XTC,
            MARIJUANA,
            HEROIN,
            LSD,

            //Tools
            SCREWDRIVER = 401,
            CROWBAR,
            LIGHTER,
            WRISTWATCH,
            HAMMER,
            RADIO,

            //Phones
            IPHONE_7 = 501,
            SAMSUNG_GALAXY_S8,
            NOKIA_3310,

            //Keys
            VEHICLE_KEY = 601,
            HOUSE_KEY,
            GARAGE_KEY,
            SHOP_KEY,
        }

        //==============================
        // On player Pickup an Item
        //==============================
        public static void PickupItem(Client player, params object[] arguments)
        {
            if (arguments.Length <= 0)
                return;

            NetHandle netValue = (NetHandle)arguments[0];

            if (!API.shared.doesEntityExist(netValue))
                return;

            if (player.position.DistanceTo(API.shared.getEntityPosition(netValue)) >= 5)
                return;

            foreach (OverworldItem i in items)
            {
                if (i.AttachedObject == netValue)
                {
                    if (API.shared.doesEntityExist(i.AttachedObject))
                    {
                        Player instance = player.getData("Instance");
                        Inventory inventory = instance.PlayerInventory;
                        Item item = i.Item;

                        API.shared.consoleOutput(item.Type.ToString());

                        //Delete item in overworld
                        API.shared.deleteEntity(i.AttachedObject);

                        //Add Item to player inventory
                        inventory.addItem(item);

                        string itemName = item.Type.ToString().ToLower();
                        itemName = itemName.First().ToString().ToUpper() + String.Join("", itemName.Skip(1));
                        itemName = itemName.Replace('_', ' ');

                        API.shared.triggerClientEvent(player, "HeadNotification", string.Format("Picked up {0}. Total: {1}", item.Type, item.Quantity));
                        items.Remove(i);
                        break;
                    }
                }
            }
        }

        //==============================
        // Dropping an item from the inventory into the overworld
        //==============================
        public static void DropItem(Client player, params object[] arguments)
        {
            //[0] = id
            //[1] = type
            //[2] = coords
            //[3] = quantity

            if (arguments.Length <= 0)
            {
                return;
            }

            int id = (int)arguments[0];
            ItemTypes type = (ItemTypes)Enum.Parse(typeof(ItemTypes), arguments[1].ToString());
            Vector3 coords = (Vector3)arguments[2];
            int quantity = Convert.ToInt32(arguments[3]);

            //Must drop it nearby
            if (coords.DistanceTo(player.position) > 6)
            {
                API.shared.sendChatMessageToPlayer(player, "~r~You're attempting to drop an item too far away. Try aiming down.");
                return;
            }

            //Check if player owns item to drop
            Player instance = player.getData("Instance");
            Inventory inventory = instance.PlayerInventory;

            foreach (KeyValuePair<int, Item> entry in inventory.CurrentItems)
            {
                Item i = entry.Value;

                if (i.Id == id && i.Type == type && i.Quantity >= quantity) //Owns item & same Type & Enough to drop
                {
                    OverworldItem newOverworldItem;

                    if (i.Data.Length <= 0) //Item is stackable, remove from stack and create new Item for overworld item
                    {
                        i.Quantity -= quantity;
                        if(i.Quantity <= 0)
                        {
                            inventory.CurrentItems.Remove(entry.Key);
                        }

                        Item item = new Item(type, quantity);
                        newOverworldItem = new OverworldItem(player, item, coords);
                        items.Add(newOverworldItem);
                        return;
                    }
                    else //Item isn't stackable, remove item from inventory and attach to new overworld item
                    {
                        inventory.CurrentItems.Remove(entry.Key);
                        newOverworldItem = new OverworldItem(player, i, coords);
                        items.Add(newOverworldItem);
                        return;
                    }
                }
            }
        }

        //==============================
        // Use item
        //==============================
        public static void UseItem(Client player, params object[] arguments)
        {
            if(arguments.Length <= 0)
            {
                return;
            }

            Player instance = player.getData("Instance");
            Inventory inventory = instance.PlayerInventory;

            if (!inventory.CurrentItems.ContainsKey((int)arguments[0]))
            {
                return;
            }

            Item item = inventory.CurrentItems.Get((int)arguments[0]);

            if (item.Quantity <= 0)
            {
                return;
            }

            //Item Types & Uses
            switch (item.Type)
            {
                case ItemTypes.COCAINE:
                    API.shared.sendChatMessageToPlayer(player,"Using some cocaine!");
                    player.armor += 10;
                    break;

                case ItemTypes.RADIO:
                    API.shared.sendChatMessageToPlayer(player, "This is a radio");
                    break;

                case ItemTypes.HOT_DOG:
                    API.shared.sendChatMessageToPlayer(player, "Eating a hot dog!");
                    player.health += 10;
                    break;
            }

            item.Quantity -= 1;

            if(item.Quantity <= 0)
            {
                inventory.CurrentItems.Remove(item.Id);
            }
        }

        //==============================
        // Add item to the item list
        //==============================
        public static void AddItem(OverworldItem item)
        {
            if (!items.Contains(item))
            {
                items.Add(item);
            }
        }

        //==============================
        // Remove item from the item list
        //==============================
        public static void RemoveItem(OverworldItem item)
        {
            if (!items.Contains(item))
            {
                items.Remove(item);
            }
        }

        //==============================
        // Get All Items (on Clientside)
        //==============================
        public static void GetItems(Client player, params object[] arguments)
        {
            if (!player.hasData("Instance"))
            {
                return;
            }
            Player instance = player.getData("Instance");
            instance.PlayerInventory.LoadItemsToLocal();
            return;
        }

        
    }
}
