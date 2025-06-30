using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using Win32API;

namespace iMonitor2.ViewModels
{
    /// <summary>
    /// Designed for monitoring csfsim, 健保讀卡機控制軟體
    /// </summary>
    internal class CsfMonitor
    {
        // 宣告變數
        private readonly System.Timers.Timer _timer1;
        private readonly System.Timers.Timer _timer2;
        private bool CsfExists;
        private bool NHICardInserted;

        public CsfMonitor(int interval)
        {
            CsfExists = false;
            NHICardInserted = false;
            this._timer1 = new System.Timers.Timer(interval);
            this._timer1.Elapsed += TimersTimer_Elapsed;
            this._timer2 = new System.Timers.Timer(interval * 5);
            this._timer2.Elapsed += TimersTimer_Elapsed2;
        }

        public void Start()
        {
            LogHelper.Instance.Info("++ Monitoring NHI CSF begins.");
            this._timer1.Start();
        }

        public void Stop()
        {
            LogHelper.Instance.Info("++ Monitoring NHI CSF ends.");
            this._timer1.Stop();
        }

        private void TimersTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            // 找到程序
            Process[] app = Process.GetProcessesByName("csfsim");
            if (app.Length == 0 && CsfExists)
            {
                // 消失了
                CsfExists = false;

                return;
            }

            if (app.Length > 0 && !CsfExists)
            {
                // 出現了
                CsfExists = true;
                Thread.Sleep(1000);
                this._timer2.Start();
                return;
            }
        }

        private void TimersTimer_Elapsed2(object? sender, ElapsedEventArgs e)
        {
            // CardType 2是健保IC卡
            int CardStatus = hisGetCardStatus(2);
            if (CardStatus == 0 && NHICardInserted)
            {
                NHICardInserted = false;
                // 健保卡剛拔掉
                //LogHelper.Instance.Info("Card Just Withdrawn!");                
            }

            if (CardStatus == 3 && !NHICardInserted)
            {
                NHICardInserted = true;
                // 健保卡剛插入
                //LogHelper.Instance.Info("Card Just Inserted!");

                // 讀取健保卡資料
                int buff = 72;
                byte[] p = new byte[buff];
                Encoding BIG5 = Encoding.GetEncoding("big5");
                int er = hisGetBasicData(p, ref buff);
                if (er != 0) return;
                string CardNo = BIG5.GetString(p, 0, 12).Trim();
                string Name = BIG5.GetString(p, 12, 20).Trim();
                string PID = BIG5.GetString(p, 32, 10).Trim();
                string Birthday = BIG5.GetString(p, 42, 7).Trim();
                string Gender = BIG5.GetString(p, 49, 1).Trim();
                string DeliverDate = BIG5.GetString(p, 50, 7).Trim();
                string VoidFlag = BIG5.GetString(p, 57, 1).Trim();
                string EmergencyPhoneNumber = BIG5.GetString(p, 58, 14).Trim();
                LogHelper.Instance.Info($"ID:{PID}; CardNO: {CardNo}; Name: {Name}; Birthday: {Birthday}; Gender: {Gender}; DeliverDate: {DeliverDate};");
                LogHelper.Instance.Info($"             VoidFlag: {VoidFlag}; EmergencyPhoneNumber: {EmergencyPhoneNumber}.") ;

                // 投送至CompanioNc6
                IntPtr hwndTarget = WM.FindWindow(null, "WebTEst2");

                string strTest = PID;
                COPYDATASTRUCT cds;
                cds.dwData = (IntPtr)100;
                cds.lpData = strTest;
                byte[] sarr = System.Text.Encoding.UTF8.GetBytes(strTest);
                int len = sarr.Length;
                cds.cbData = len + 1;
                _ = WM.SendMessage(hwndTarget, WM.WM_COPYDATA, 0, ref cds);

            }

        }

        /// <summary>
        /// API name        hisGetCardStatus
        /// 語法            int hisGetCardStatus(int CardType);
        /// 目的            讀出卡片狀態
        /// 參數說明        [in] CardType: 傳入讀取卡片種類， 定義如下
        ///                 1: 安全模組檔
        ///                 2: 健保IC卡
        ///                 3: 醫事人員卡
        /// 回傳值          回傳整數值， 定義如下:
        ///                當CardType = 1， 回傳值， 定義如下:
        ///                 4000：讀卡機Timeout
        ///                 0：卡片未置入
        ///                 1：安全模組尚未與健保局IDC認證
        ///                 2：安全模組與健保局IDC認證成功
        ///                 9：所置入非安全模組檔
        ///                當CardType = 2， 回傳值， 定義如下:
        ///                 4000：讀卡機Timeout
        ///                 0：卡片未置入
        ///                 1：健保IC卡尚未與安全模組認證
        ///                 2：健保IC卡與安全模組認證成功
        ///                 3：健保IC卡與醫事人員卡認證成功
        ///                 4：健保IC卡PIN認證成功
        ///                 5：健保IC卡與健保局IDC認證成功
        ///                 9：所置入非健保IC卡
        ///                當CardType = 3， 回傳值， 定義如下:
        ///                 4000：讀卡機Timeout
        ///                 0：卡片未置入
        ///                 1：醫事人員卡尚未與安全模組認證
        ///                 2：醫事人員卡與安全模組認證成功(PIN尚未認證)
        ///                 3：醫事人員卡PIN認證成功
        ///                 9：所置入非醫事人員卡
        /// </summary>
        /// <param name="CardType"></param>
        /// <returns></returns>
        [DllImport("CsHIS.dll", SetLastError = false)]
        private static extern int hisGetCardStatus(int CardType);

        /// <summary>
        /// API name        hisGetBasicData
        /// 語法            ERRORCODE hisGetBasicData(char* pBuffer， int* iBufferLen);
        /// 目的            讀取不需個人PIN碼之基本資料
        /// 參數說明         [out] pBuffer：為HIS準備之buffer，需可存入「pBuffer回傳內容」所稱之欄位值。欄位存入的順序，如「pBuffer回傳內容」所述。
        ///                 [in/out] iBufferLen：為HIS準備之buffer，HIS呼叫此API時，傳入準備的buffer長度；CS API亦利用此buffer傳出填入到buffer中的資料長度(buffer的尾端不必補\0)。
        /// pBuffer回傳內容  回傳內容及順序如下: 共72 Bytes
        ///                 卡片號碼(1-12)
        ///                 姓名(13-32)
        ///                 身分證號或身分證明文件號碼(33-42)
        ///                 出生日期(43-49)
        ///                 性別(50)
        ///                 發卡日期(51-57)
        ///                 卡片註銷註記(58)
        ///                 緊急聯絡電話(59-72)
        /// 回傳值           ERRORCODE為整數值
        ///                 0：無任何錯誤
        ///                 4000：讀卡機timeout
        ///                 4013：未置入健保IC卡
        ///                 4029：IC卡權限不足
        ///                 4033：所置入非健保IC卡
        ///                 4050：安全模組尚未與IDC認證
        ///                 其他：詳見回傳值代碼對照表
        /// 使用場合         民眾持HC卡進行掛號時，HIS可不需個人PIN碼即可取得健保IC卡內基本資料段之資料
        /// </summary>
        /// <param name="pBuffer"></param>
        /// <param name="iBufferLen"></param>
        /// <returns></returns>
        [DllImport("CsHIS.dll", SetLastError = false)]
        private static extern int hisGetBasicData(byte[] pBuffer, ref int iBufferLen);

    }
}
