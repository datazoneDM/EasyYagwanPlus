using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DevExpress.UserSkins;
using DevExpress.Skins;

namespace Easy약관Plus
{

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) 
        {
            //////Application.EnableVisualStyles();
            //////Application.SetCompatibleTextRenderingDefault(false);
            //////BonusSkins.Register();
            //////SkinManager.EnableFormSkins();


            //////Application.Run(new frmEasy약관(args));
            ///

            // 1. 전역 UI 스레드 예외 처리
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // 2. AppDomain 예외 처리 (DLL 로드 실패 등 시작 지점의 치명적 오류 캡처)
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // 외부 UI 라이브러리 초기화 부분
                BonusSkins.Register();
                SkinManager.EnableFormSkins();

                // 메인 폼 실행
                Application.Run(new frmEasy약관(args));
            }
            catch (Exception ex)
            {
                // 3. Main 내부에서 잡히는 예외 처리
                ShowErrorDialog(ex);
            }

        }

        // 예외 발생 시 화면에 원인을 띄워주는 공통 메서드
        private static void ShowErrorDialog(Exception ex)
        {
            MessageBox.Show(
                $"[Easy약관Plus 실행 오류]\n\n" +
                $"어떤 파일이 없나요?: {ex.Message}\n\n" +
                $"상세 위치:\n{ex.StackTrace}",
                "시스템 오류 확인",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        // AppDomain 예외 핸들러
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                ShowErrorDialog(ex);
            }
        }

        // UI 스레드 예외 핸들러
        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            ShowErrorDialog(e.Exception);
        }


    }
}
