using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lobby.OAuth
{
    public class PlatformInfo
    {
        /// <summary>
        /// 소셜 플랫폼 아이디
        /// </summary>
        public string PlatformId { get; set; }

        /// <summary>
        /// 소셜 플랫폼 인증시 발급되는 토큰값
        /// </summary>
        public string PlatformToken { get; set; }

        /// <summary>
        /// 게임센터 인증시 필요한 추가 정보를 전달
        /// </summary>
        public GameCenterAuth gamecenter;

        /// <summary>
        /// 프로필 이미지URL
        /// </summary>
        public string ProfileImgUrl;
    }

    public class GameCenterAuth
    {
        public string BundleId { get; set; }

        public string PublicKeyUrl { get; set; }

        public string Signature { get; set; }

        public string Salt { get; set; }

        public string Timestamp { get; set; }
    }



    public class ExternalProvider
    {

    }
}
