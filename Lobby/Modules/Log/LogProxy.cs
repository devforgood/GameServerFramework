using core;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class LogProxy
    {

        public static async Task Write(Session session, PlayerLog msg)
        {
            var playerInfo = await PlayerLog.GetPlayerInfo(session);
            if(playerInfo == null)
            {
                Log.Error($"error {JsonConvert.SerializeObject(msg)}");
                return;
            }
            msg.Copy(playerInfo);
            await WebAPIClient.Web.writeLog(session.player_id, "/log/writeActionLog", JsonConvert.SerializeObject(msg));
        }

        /// <summary>
        /// 구매 로그를 기록
        /// </summary>
        /// <param name="session"></param>
        /// <param name="currency">통화 코드 ISO 4217 에 명시된 통화 코드 
        //        OS 별 currency 설정에 대한 제약 없음
        //        여러 국가/마켓에서 서비스 될 경우 currency를 하나로 맞추어 보내야한다는 제 약 없음
        //       OS/국가코드/마켓코드에 상관없이 실제로 스토어 결제가 이루어진 통화 코드로  설정 </param>
        /// <param name="price">VAT 포함 상품 금액</param>
        /// <param name="marketOrderId">마켓 주문 아이디</param>
        /// <param name="marketProductId">마켓 상품 아이디</param>
        /// <param name="marketPurchaseTime">마켓 구매 처리 시각 epoch time (unit : milli-seconds) </param>
        /// <param name="marketPurchaseData">마켓 구매 부가 정보</param>
        /// <param name="purchasePt">구매로 인해 생성된 포인 트</param>
        /// <param name="purchaseCount">구매 회차</param>
        /// <param name="purchaseToken">구매 토큰 구글 결제 시에만 설정한다. 구글 결제 취소 건 확인 시 필요</param>
        /// <returns></returns>
        public static async Task writePurchaseLog(Session session, string currency, decimal price, string marketOrderId, string marketProductId, long marketPurchaseTime, Dictionary<string, object> marketPurchaseData, long purchasePt, long purchaseCount, string purchaseToken)
        {
            var playerInfo = await PlayerLog.GetPlayerInfo(session);
            if (playerInfo == null)
            {
                Log.Error($"error {session.player_id}, {currency}, {price}, {marketOrderId}, {marketProductId}, {marketPurchaseTime}, {JsonConvert.SerializeObject(marketPurchaseData)}, {purchasePt}, {purchaseCount}");
                return;
            }

            var msg = new PurchaseLog(playerInfo)
            {
                currency = currency,
                price = price,
                marketOrderId = marketOrderId,
                marketProductId = marketProductId,
                marketPurchaseTime = marketPurchaseTime,
                marketPurchaseData = marketPurchaseData,
                purchasePt = purchasePt,
                purchaseCount = purchaseCount,
                purchaseToken = purchaseToken,
            };

            await WebAPIClient.Web.writeLog(session.player_id, "/log/writePurchaseLog", JsonConvert.SerializeObject(msg));
        }

        /// <summary>
        /// 현금 구매에 의해 획득되는 재화를 cash item 이라고 칭하고 있다. 
        /// 이 API 는 cash item의 변동 로그를 기록한다.cash item 에 변동(지급/소진/회수)이 생긴 경우 호출한다.재화 변동 로그는 PlayerLog 의 일부이며 (src, code) 로 (p1, cashitem) 를 사용한다.cash item 변동 로그는 3개월간 조회 가능하며 5년 후 자동 삭제 된다.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="paid">재화 유무료 구분 y : 유료 (유료+무료 동시 사용 포함) 
        //n: 무료
        //u : 알수없음(유무료 미구분 재화)    </param>
        /// <param name="rCurrency">리소스 통화 코드 gem : 보석 </param>
        /// <param name="delta">재화 변동량 (+/-) 증가의 경우 양수로 설정 / 감소의 경우 음수로 설정</param>
        /// <param name="paidDelta">유료재화 변동량 (+/-)  증가의 경우 양수로 설정 / 감소의 경우 음수로 설정</param>
        /// <param name="amount">현재 재화 보유량</param>
        /// <param name="modType">획득/소진 구분 코드  add, sub</param>
        /// <param name="reason"></param>
        /// <param name="subReason"></param>
        /// <param name="resourceAttr1"></param>
        /// <param name="resourceAttr2"></param>
        /// <param name="resourceAttr3"></param>
        /// <param name="resourceAttr4"></param>
        /// <param name="memo"></param>
        /// <returns></returns>
        public static async Task writeCashItemLog(Session session, string paid, string rCurrency, int delta, int paidDelta, int amount, string modType, string reason, string subReason, string resourceAttr1, string resourceAttr2, string resourceAttr3, string resourceAttr4, string memo)
        {
            long modTime = DateTime.UtcNow.ToEpochTime();

            var playerInfo = await PlayerLog.GetPlayerInfo(session);
            if (playerInfo == null)
            {
                Log.Error($"error {session.player_id}, {paid}, {rCurrency}, {delta}, {paidDelta}, {amount}, {modType}, {modTime}, {reason}, {subReason}, {resourceAttr1}, {resourceAttr2}, {resourceAttr3}, {resourceAttr4}, {memo}");
                return;
            }

            var msg = new CashItemLog(playerInfo)
            {
                paid = paid,
                rCurrency = rCurrency,
                delta = delta,
                paidDelta = paidDelta,
                amount = amount,
                modType = modType,
                modTime = modTime,
                reason = reason,
                subReason = subReason,
                resourceAttr1 = resourceAttr1,
                resourceAttr2 = resourceAttr2,
                resourceAttr3 = resourceAttr3,
                resourceAttr4 = resourceAttr4,
                memo = memo,
            };

            await WebAPIClient.Web.writeLog(session.player_id, "/log/writeCashItemLog", JsonConvert.SerializeObject(msg));

        }

        /// <summary>
        /// 인 게임 재화 변동 로그를 기록한다. 
        /// 인 게임 재화에 변동(지급/소진/회수)이 생긴 경우 호출한다.
        /// 통화, 유통량, 지급소진의 구분이 중요한 경우 사용된다.
        /// 인 게임 재화 변동 로그는 로그 시스템 내에서 playerLog로 관리된다. (src p1, code resource)
        /// 인 게임 재화 변동 로그는 60일 후 자동 삭제된다. 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="paid">재화 유무료 구분 y : 유료 (유료+무료 동시 사용 포함) 
        //n: 무료
        //u : 알수없음(유무료 미구분 재화)    </param>
        /// <param name="rCurrency">리소스 통화 코드 gem : 보석 </param>
        /// <param name="delta">재화 변동량 (+/-) 증가의 경우 양수로 설정 / 감소의 경우 음수로 설정</param>
        /// <param name="paidDelta">유료재화 변동량 (+/-)  증가의 경우 양수로 설정 / 감소의 경우 음수로 설정</param>
        /// <param name="amount">현재 재화 보유량</param>
        /// <param name="modType">획득/소진 구분 코드  add, sub</param>
        /// <param name="reason"></param>
        /// <param name="subReason"></param>
        /// <param name="resourceAttr1"></param>
        /// <param name="resourceAttr2"></param>
        /// <param name="resourceAttr3"></param>
        /// <param name="resourceAttr4"></param>
        /// <param name="memo"></param>
        /// <returns></returns>
        public static async Task writeResourceLog(Session session, string paid, string rCurrency, int delta, int paidDelta, int amount, string modType, string reason, string subReason, string resourceAttr1 = "", string resourceAttr2 = "", string resourceAttr3 = "", string resourceAttr4 = "", string memo = "")
        {
            long modTime = DateTime.UtcNow.ToEpochTime();

            var playerInfo = await PlayerLog.GetPlayerInfo(session);
            if (playerInfo == null)
            {
                Log.Error($"error {session.player_id}, {paid}, {rCurrency}, {delta}, {paidDelta}, {amount}, {modType}, {modTime}, {reason}, {subReason}, {resourceAttr1}, {resourceAttr2}, {resourceAttr3}, {resourceAttr4}, {memo}");
                return;
            }

            var msg = new ResourceLog(playerInfo)
            {
                paid = paid,
                rCurrency = rCurrency,
                delta = delta,
                paidDelta = paidDelta,
                amount = amount,
                modType = modType,
                modTime = modTime,
                reason = reason,
                subReason = subReason,
                resourceAttr1 = resourceAttr1,
                resourceAttr2 = resourceAttr2,
                resourceAttr3 = resourceAttr3,
                resourceAttr4 = resourceAttr4,
                memo = memo,
            };

            await WebAPIClient.Web.writeLog(session.player_id, "/log/writeResourceLog", JsonConvert.SerializeObject(msg));

        }

        /// <summary>
        /// 아이템 변동 로그를 기록한다. 
        /// 게임 내 상태의 변화(강화, 합성, 삭제 등)나 속성/유형에 따른 분류/관리가 필요한 경우 사용한다.아이템 변동 로그는 로그 시스템 내에서 playerLog로 관리된다. (src g1, code item) 아이템 변동 로그는 60일 후 자동 삭제된다.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="itemType">       아이템 유형        </param>
        /// 캐릭터 코스튬 
        /// 캐릭터 조각
        /// 미션_데일리 미션 상자
        /// 각성석
        /// 상점_일반 상자
        /// 상점_광고_광고 보상 상자
        /// 상점_광고_메달 쿠폰 상자
        /// 인게임 일반 아이템
        /// <param name="itemId">아이템 아이디</param>
        /// <param name="permanence">아이템 영구성 여부</param>
        /// <param name="itemAttr1">아이템 추가 속성 1</param>
        /// <param name="quantity">아이템 변동 수량</param>
        /// <param name="rCurrency">아이템 변동 비용에 리소스 화폐 단위</param>
        /// <param name="cost">아이템 변동에 따른 비용</param>
        /// <param name="paidCost">아이템 변동에 따른 비용(유료재화)</param>
        /// <param name="modType">아이템 변동 구분 코드  add, sub</param>
        /// <param name="reason">아이템 변동 사유</param>
        /// <param name="subReason">아이템 변동 상세 사유</param>
        /// <param name="memo"></param>
        /// <returns></returns>
        public static async Task writeItemLog(Session session, string itemType, string itemId, string permanence, string itemAttr1, long quantity, string rCurrency, long cost, long paidCost, string modType, string reason, string subReason, string memo)
        {
            long modTime = DateTime.UtcNow.ToEpochTime();

            var playerInfo = await PlayerLog.GetPlayerInfo(session);
            if (playerInfo == null)
            {
                Log.Error($"error {session.player_id}, {itemType}, {itemId}, {permanence}, {itemAttr1}, {quantity}, {rCurrency}, {modTime}, {cost}, {paidCost}, {modType}, {reason}, {subReason}, {memo}, {modTime}");
                return;
            }

            var msg = new ItemLog(playerInfo)
            {
                itemType = itemType,
                itemId = itemId,
                permanence = permanence,
                itemAttr1 = itemAttr1,
                quantity = quantity,
                rCurrency = rCurrency,
                cost = cost,
                paidCost = paidCost,
                modType = modType,
                reason = reason,
                subReason = subReason,
                memo = memo,
                modTime = modTime,
            };

            await WebAPIClient.Web.writeLog(session.player_id, "/log/writeItemLog", JsonConvert.SerializeObject(msg));
        }


        /// <summary>
        /// 판 시작/종료 로그를 기록한다. 
        /// 게임 한판(round/match 등)이 시작할 때와 끝났을 때에 호출한다.
        /// 판 시작/종료 로그는 로그 시스템 내에서 playerLog로 관리된다. (src g1, code round)
        /// 판 시작/종료 로그는 60일 후 자동 삭제된다. 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="gameMode">게임 모드 구분 캐슬번 / 킬더킹</param>
        /// <param name="gameModeDtl">게임 모드 하위 구분</param>
        /// <param name="roundAttr1">라운드 추가 속성 1</param>
        /// <param name="resultTp">게임 결과 유형</param>
        /// [시작 시] 10: 시작 
        /// [종료 시] 21: 성공, 22: 실패, 23: 포기, 24: 포기 재시작, 26(무승부)
        /// <param name="resultAmt">게임 결과 보상량</param>
        /// <param name="startTime">게임 시작 시각 </param>
        /// <param name="endTime">게임 종료 시각</param>
        /// <param name="character1Id">첫 번째 캐릭터 아이디</param>
        /// <param name="character2Id"></param>
        /// <param name="character3Id"></param>
        /// <param name="character4Id"></param>
        /// <param name="character5Id"></param>
        /// <param name="character6Id"></param>
        /// <param name="character1Lv">첫 번째 캐릭터 레벨</param>
        /// <param name="character2Lv"></param>
        /// <param name="character3Lv"></param>
        /// <param name="character4Lv"></param>
        /// <param name="character5Lv"></param>
        /// <param name="character6Lv"></param>
        /// <param name="memo">메모</param>
        /// <returns></returns>
        public static async Task writeRoundLog(Session session, string gameMode, string gameModeDtl, string roundAttr1, string resultTp, long resultAmt, long startTime, long endTime, List<string> characterId, List<int> characterLv, string memo)
        {
            long modTime = DateTime.UtcNow.ToEpochTime();

            var playerInfo = await PlayerLog.GetPlayerInfo(session);
            if (playerInfo == null)
            {
                Log.Error($"error {session.player_id}, {gameMode}, {gameModeDtl}, {roundAttr1}, {resultTp}, {resultAmt}, {startTime}, {endTime}, {JsonConvert.SerializeObject(characterId)}, {JsonConvert.SerializeObject(characterLv)}, {memo}, {modTime}");
                return;
            }

            var msg = new RoundLog(playerInfo)
            {
                gameMode = gameMode,
                gameModeDtl = gameModeDtl,
                roundAttr1 = roundAttr1,
                resultTp = resultTp,
                resultAmt = resultAmt,
                startTime = startTime,
                endTime = endTime,
                character1Id = characterId.Count > 0 ? characterId[0] : "",
                character2Id = characterId.Count > 1 ? characterId[1] : "",
                character3Id = characterId.Count > 2 ? characterId[2] : "",
                character4Id = characterId.Count > 3 ? characterId[3] : "",
                character5Id = characterId.Count > 4 ? characterId[4] : "",
                character6Id = characterId.Count > 5 ? characterId[5] : "",
                character1Lv = characterLv.Count > 0 ? characterLv[0] : 0,
                character2Lv = characterLv.Count > 1 ? characterLv[1] : 0,
                character3Lv = characterLv.Count > 2 ? characterLv[2] : 0,
                character4Lv = characterLv.Count > 3 ? characterLv[3] : 0,
                character5Lv = characterLv.Count > 4 ? characterLv[4] : 0,
                character6Lv = characterLv.Count > 5 ? characterLv[5] : 0,
                memo = memo,
                modTime = modTime,
            };

            await WebAPIClient.Web.writeLog(session.player_id, "/log/writeRoundLog", JsonConvert.SerializeObject(msg));
        }

        /// <summary>
        /// 액션 로그를 기록한다. 
        /// 게임 단에서 정의된 액션 발생 시 호출한다.
        /// 액션 로그는 로그 시스템 내에서 playerLog로 관리된다. (src g1, code action)
        /// 액션 로그는 60일 후 자동 삭제된다.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="category">카테고리 </param>
        /// <param name="action">액션 </param>
        /// <param name="label">분류 </param>
        /// <param name="actionAttr1"></param>
        /// <param name="actionAttr2"></param>
        /// <param name="gameLogYn"></param>
        /// <returns></returns>
        public static async Task writeActionLog(Session session, string category, string action, string label, string actionAttr1 = "", string actionAttr2 = "", string gameLogYn = "Y")
        {
            long modTime = DateTime.UtcNow.ToEpochTime();

            var playerInfo = await PlayerLog.GetPlayerInfo(session);
            if (playerInfo == null)
            {
                Log.Error($"error {session.player_id}, {category}, {action}, {label}, {actionAttr1}, {actionAttr2}, {gameLogYn}, {modTime}");
                return;
            }

            var msg = new ActionLog(playerInfo)
            {
                category = category,
                action = action,
                label = label,
                actionAttr1 = actionAttr1,
                actionAttr2 = actionAttr2,
                gameLogYn = gameLogYn,
                modTime = modTime,
            };

            await WebAPIClient.Web.writeLog(session.player_id, "/log/writeActionLog", JsonConvert.SerializeObject(msg));
        }

    }
}
