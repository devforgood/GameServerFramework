using GameService;
using Google.Protobuf.Collections;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class ShopManager
    {
        private static async Task _Refresh(Session session, List<Models.Shop> shops, List<Models.Shop> db_shop, KeyValuePair<int, JShopListData> shop_data)
        {
            var except_item_list = new List<JShopItemListData>();
            var characters = await CharacterCache.Instance.GetEntities(session.member_no, session.user_no, true);
            var candidate_shop_item_list_data = ACDC.ShopItemListData.Where(x => shop_data.Value.ProductGroupId.Contains(x.Value.GroupId)).Select(x => x.Value).ToList();
            foreach (var shop_item_data in candidate_shop_item_list_data)
            {
                if (characters.IsAvailable(shop_item_data.ItemId) == false)
                {
                    except_item_list.Add(shop_item_data);
                }
            }



            for (int i = 0; i < shop_data.Value.ProductGroupId.Length; ++i)
            {
                int shop_item_id = 0;
                int shop_item_count = 0;

                var shop_item_list_data = ACDC.ShopItemListData.Where(x => x.Value.GroupId == shop_data.Value.ProductGroupId[i]).Select(x => x.Value).Except(except_item_list).ToList();
                if (shop_item_list_data.Count != 0) // empty 슬롯 처리
                {
                    var rand_value = core.ThreadSafeRandom.Get().Next(0, shop_item_list_data.Count);
                    var selected_shop_item = shop_item_list_data[rand_value];

                    int quantity = 0;
                    if (selected_shop_item.Quantity.Length > 1)
                    {
                        quantity = core.ThreadSafeRandom.Get().Next(selected_shop_item.Quantity[0], selected_shop_item.Quantity[1]);
                    }
                    else if (selected_shop_item.Quantity.Length == 1)
                    {
                        quantity = selected_shop_item.Quantity[0];
                    }

                    except_item_list.Add(selected_shop_item);

                    shop_item_id = selected_shop_item.Id;
                    shop_item_count = quantity;
                }

                if (i < db_shop.Count)
                {
                    db_shop[i].occ_time = DateTime.UtcNow;

                    db_shop[i].shop_item_id = shop_item_id;
                    db_shop[i].quantity = shop_item_count;
                    db_shop[i].purchase_count = 0;

                    await ShopCache.Instance.UpdateEntity(session.member_no, db_shop[i]);
                }
                else
                {
                    var shop_item = new Models.Shop();
                    shop_item.user_no = session.user_no;
                    shop_item.shop_id = shop_data.Value.Id;
                    shop_item.occ_time = DateTime.UtcNow;

                    shop_item.shop_item_id = shop_item_id;
                    shop_item.quantity = shop_item_count;
                    shop_item.purchase_count = 0;

                    shops.Add(shop_item);
                    await ShopCache.Instance.InsertEntity(session.member_no, shop_item);
                }
            }
        }
        public static async Task<bool> Refresh(Session session, List<Models.Shop> shops, bool is_only_check)
        {
            // 유효하지 않은 미션 리셋
            foreach (var shop_data in ACDC.ShopListData)
            {
                if (shop_data.Value.Enable == false)
                    continue;

                var db_shop = shops.Where(x => x.shop_id == shop_data.Value.Id).ToList();


                bool is_reset = false;
                if (db_shop.Count > 0)
                {
                    foreach (var shop_item in db_shop)
                    {
                        if (shop_data.Value.ResetType == (int)ShopResetType.Daily)
                        {
                            if (DateTime.UtcNow.Date != shop_item.occ_time.Date)
                            {
                                is_reset = true;
                                break;
                            }
                        }
                        else if(shop_data.Value.ResetType == (int)ShopResetType.Monthly)
                        {
                            if (DateTime.UtcNow.Date.Month != shop_item.occ_time.Date.Year 
                                || DateTime.UtcNow.Date.Month != shop_item.occ_time.Date.Month)
                            {
                                is_reset = true;
                                break;
                            }
                        }
                    }
                }

                // 시간 만료이거나, 스크립트 데이터와 수량이 다를 경우
                if (is_reset || db_shop.Count < shop_data.Value.ProductGroupId.Length)
                {
                    if (is_only_check)
                    {
                        return true;
                    }
                    else
                    {
                        await _Refresh(session, shops, db_shop, shop_data);
                    }
                }

                // 스크립트 데이터보다 디비, 캐시 데이터가 많을 경우 삭제
                if (db_shop.Count > shop_data.Value.ProductGroupId.Length)
                {
                    if (is_only_check)
                    {
                        return true;
                    }
                    else
                    {
                        for (int i = shop_data.Value.ProductGroupId.Length; i < db_shop.Count; ++i)
                        {
                            await ShopCache.Instance.RemoveEntity(session.member_no, db_shop[i]);
                            await ShopQuery.Remove(session.member_no, db_shop[i]);

                            shops.Remove(db_shop[i]);
                        }
                    }
                }
            }
            return false;

        }

        public static async Task<UserShops> GetShopLock(Session session)
        {
            var userShops = new UserShops();
            List<Models.Shop> shops = null;
            shops = await ShopCache.Instance.GetEntities(session.member_no, session.user_no, true);
            if (await Refresh(session, shops, true))
            {
                await using (var mylock = await RedLock.CreateLockAsync($"lock:session:{session.session_id}"))
                {
                    shops = await ShopCache.Instance.GetEntities(session.member_no, session.user_no, true);
                    await Refresh(session, shops, false);
                }
            }

            if (shops.Count > 0)
            {
                foreach (var shop in shops)
                {
                    var shopInfo = userShops.Shops.Where(x => x.ShopId == shop.shop_id).FirstOrDefault();
                    if (shopInfo == null || shopInfo == default(ShopInfo))
                    {
                        shopInfo = new ShopInfo();
                        shopInfo.ShopId = shop.shop_id;
                        userShops.Shops.Add(shopInfo);
                    }

                    shopInfo.Items.Add(new ShopItemInfo()
                    {
                        ShopItemId = shop.shop_item_id,
                        Quantity = shop.quantity,
                        PurchaseCount = shop.purchase_count
                    });
                }
            }

            return userShops;
        }

        public static async Task<(ErrorCode, ItemList, Goods)> BuyItem(Session session, int shopItemId, int shopId)
        {
            var Item = new ItemList();
            var AccountGoods = new Goods();

            // 상점 아이템 목록 체크
            var shop_item_data = ACDC.ShopItemListData[shopItemId];
            if (shop_item_data == null || shop_item_data == default(JShopItemListData))
            {
                return (ErrorCode.WrongParam, Item, AccountGoods);
            }

            var item_data = ACDC.GameItemData[shop_item_data.ItemId];
            if(item_data == null || item_data == default(JGameItemData))
            {
                return (ErrorCode.WrongParam, Item, AccountGoods);
            }

            await using (var mylock = await RedLock.CreateLockAsync($"lock:session:{session.session_id}"))
            {
                // 유저 상점 리스트 얻기
                var shops = await ShopCache.Instance.GetEntities(session.member_no, session.user_no, true);

                // 상점 목록 갱신
                await Refresh(session, shops, true);

                // 구매하려는 아이템 목록에 있는지 체크
                var shop_item = shops.Where(x => x.shop_id == shopId && x.shop_item_id == shopItemId).FirstOrDefault();
                if (shop_item == null || shop_item == default(Models.Shop))
                {
                    return (ErrorCode.NotExist, Item, AccountGoods);
                }

                // 구매 한정 체크
                if (shop_item.purchase_count >= shop_item_data.PurchaseLimitedCount)
                {
                    return (ErrorCode.OverLimit, Item, AccountGoods);
                }

                await using (var user = await UserCache.GetUser(session.member_no, session.user_no, true, true, false))
                await using (var character = await CharacterCache.Instance.GetEntity(session.member_no, session.character_no, true, true, false))
                {
                    // 지불 금액 체크 및 소모
                    if (shop_item_data.PriceType != 0 && shop_item_data.PriceType != (int)GameItemId.Cash) // 0은 무료
                    {
                        var reason = "S_BUY_ITEM";
                        var sub_reason = "";
                        if (item_data.Item_Type == (int)GameItemType.CharacterPiece)
                        {
                            reason = "S_BUY_SMASHER";
                            sub_reason = item_data.LinkId.ToString();
                        }
                        if(shop_item_data.ShopItemType == (int)ShopItemType.Goods)
                        {
                            reason = "S_BUY_MONEY";
                            sub_reason = item_data.LinkId.ToString();
                        }

                        if (Inventory.UseGoods(session, user, shop_item_data.PriceType, shop_item_data.PriceValue * shop_item.quantity, new LogReason(reason, sub_reason)) == false)
                        {
                            return (ErrorCode.NotEnough, Item, AccountGoods);
                        }
                    }

                    // 아이템 지급
                    shop_item.purchase_count += 1;
                    await ShopCache.Instance.UpdateEntity(session.member_no, shop_item);

                    await Inventory.Insert(session, user, character, shop_item_data.ItemId, shop_item.quantity, new LogReason(shop_item_data.logName, shop_item_data.LinkId.ToString()), Item);

                    AccountGoods.Set(user);

                    if(shop_item_data.logName == "AD")
                    {
                        _ = LogProxy.writeActionLog(session, "광고시청", "광고", shop_item_data.LinkId.ToString()).ConfigureAwait(false);
                    }
                }
            }

            return (ErrorCode.Success, Item, AccountGoods);
        }



    }
}
