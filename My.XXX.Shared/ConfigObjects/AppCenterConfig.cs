namespace My.XXX.Shared
{
    public class AppCenterConfig
    {
        private string _userInfoAPI;
        public string UserInfoAPI
        {
            get
            {
                return null != _userInfoAPI ? _userInfoAPI.TrimEnd('/') : _userInfoAPI;
            }
            set
            {
                _userInfoAPI = value;
            }
        }
        public string LoginUrl { get; set; }
        public string AppSecret { get; set; }
        public string AppCode { get; set; }
        public int TokenExpireTime { get; set; }
    }
}