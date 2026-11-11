using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;
using System.Linq;
using Renci.SshNet;
using System.Threading;
using System.Data.SqlClient;
using System.Net;
using System.Data;
using System.Text;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace Easy약관Plus
{
    public static class NetworkManager
    {
        public static async Task<string> 공통설정파일_로드()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // =======================================================
                    // 기초 환경 변수 일괄 세팅 (프로그램 전체 기준점)
                    // =======================================================
                    if (string.IsNullOrEmpty(저장.사용컴퓨터))
                    {
                        저장.사용컴퓨터 = SystemInformation.ComputerName.ToLower();
                    }
                    저장.DB설정파일path = System.IO.Path.Combine(Application.StartupPath, "localDB.txt");
                    저장.기본파일폴더 = @"d:\Easy약관Plus\Input\";

                    // =======================================================
                    // 이미 설정값이 있다면 중복 로드 방지 (속도 최적화)
                    // =======================================================
                    if (저장.설정값 != null && 저장.설정값.Count > 0)
                    {
                        return "성공"; // 이미 로드되어 있으면 바로 통과!
                    }

                    저장.설정값 = new Dictionary<string, string>();
                    if (new FileInfo(저장.DB설정파일path).Exists)
                    {
                        string[] 설정파일 = File.ReadAllLines(저장.DB설정파일path, Encoding.GetEncoding(949));
                        foreach (string tmp설정 in 설정파일)
                        {
                            string[] 분리 = tmp설정.Split('\t');
                            if (분리.Length < 2) continue;

                            string key = 분리[0].Trim();
                            string value = 분리[1].Trim();

                            // 기존의 복잡한 분리 로직 (접속, FTP, 모바일 등)
                            if (key == "접속" || key == "FTP" || key == "소켓" || key == "모바일" || key == "약관" || key == "PDF")
                            {
                                if (분리.Length < 3) continue;
                                string 키1 = 분리[1].Trim();
                                string 값1 = 분리[2].Trim();

                                if (key == "접속" || key == "소켓")
                                {
                                    키1.Split('|').ToList().ForEach(x => {
                                        if (!저장.설정값.ContainsKey($"{key}{x}"))
                                            저장.설정값.Add($"{key}{x}", 값1);
                                    });
                                }
                                else
                                {
                                    if (!저장.설정값.ContainsKey($"{key}{키1}"))
                                        저장.설정값.Add($"{key}{키1}", 값1);
                                }
                            }
                            else if (key == "병렬")
                            {
                                value.Split('|').Where(x => x.Contains("main") || x.Contains("sub")).ToList().ForEach(x => {
                                    if (!저장.설정값.ContainsKey($"병렬{x}"))
                                        저장.설정값.Add($"병렬{x}", x.Contains("main") ? "main" : "sub");
                                });
                            }
                            else
                            {
                                if (!저장.설정값.ContainsKey(key.ToUpper()))
                                    저장.설정값.Add(key.ToUpper(), value);
                            }
                        }
                    }

                    // 재시작 설정 세팅
                    if (저장.설정값.TryGetValue("재시작", out string tmp정기재시작))
                        저장.정기재시작 = Convert.ToInt16(tmp정기재시작);

                    // 여기서 약관DB를 전역변수에 확실히 고정! (다른 폼에서 두 번 할당할 필요 없음)
                    if (저장.설정값.TryGetValue("약관전체", out string 약관접속))
                        저장.약관DB = 약관접속;

                    if (저장.설정값.TryGetValue("에디터경로32", out string 에디터경로32))
                    {
                        if(new FileInfo(에디터경로32).Exists)
                            저장.에디터경로 = 에디터경로32;
                        else
                        {
                            if (저장.설정값.TryGetValue("에디터경로64", out string 에디터경로64))
                            {
                                저장.에디터경로 = 에디터경로64;
                            }
                        }
                    }
                        

                    return "성공";
                }
                catch (Exception)
                {
                    return "실패";
                }
            });
        }

        public static string 고객사선택(string tmp고객사, string 현재컴)
        {
            string 선택고객사 = tmp고객사;
            //저장.사용컴퓨터
            switch (현재컴)
            {
                case "kbmain":
                case "kbsub1":
                case "kbsub2":
                case "kbsub3":
                case "kbsub4":
                case "kbsub5":
                case "dps77":    //종우컴(전체약관 업로드시 사용)
                    선택고객사 = "KB2";
                    break;
            }
            return 선택고객사;
        }

        // ===================================================================================================
        // 공통 실행 래퍼 (에러 낚아채서 로그로 던짐)
        // ===================================================================================================
        public static async Task<string> 실행(Func<Task<string>> func, string name, Action<string, int> logCallback)
        {
            string 실행여부 = "";
            try
            {
                실행여부 = await func();
            }
            catch (Exception e1)
            {
                실행여부 = "실패";

                var 에러 = new System.Diagnostics.StackTrace(e1, true);
                int 라인 = 0;
                if (에러.FrameCount > 0)
                {
                    라인 = 에러.GetFrame(0).GetFileLineNumber();
                }

                string 오류로그 = string.Format("{0:yy.MM.dd HH:mm:ss} [오류] {1} (메소드 : {2}, Line : {3})", DateTime.Now, e1.Message, name, 라인);
                logCallback?.Invoke(오류로그, 1);
            }
            return 실행여부;
        }

        // ===================================================================================================
        // 1. SFTP 업로드 (KB전용)
        // ===================================================================================================
        public static async Task<string> FTP_FILE_UPLOAD2(
            현재상태 상태,
            List<레코드> 레코드s,
            Action<string, int> logCallback,
            Action<string[]> progressCallback)
        {
            string 실행여부 = "성공";

            string tmpFTP설정값 = "";
            if (!저장.설정값.TryGetValue($"FTP{고객사선택(상태.고객사, 저장.사용컴퓨터)}", out tmpFTP설정값))
                throw new Exception("FTP설정값 미존재, FTP전송 중단");

            string 고객사 = 고객사선택(상태.고객사, 저장.사용컴퓨터) ?? "";
            string[] 계정 = tmpFTP설정값.Split('|').Select(P => P.Trim()).ToArray();

            FTP상태 ftp상태 = new FTP상태();
            if (계정.Length == 4)
                ftp상태 = new FTP상태 { IP = 계정[0], PORT = Convert.ToInt32(계정[1]), ID = 계정[2], PW = 계정[3] };
            else
                ftp상태 = new FTP상태 { IP = 계정[0], ID = 계정[1], PW = 계정[2] };

            string 기본FTP경로 = 상태.고객사 == "DB" ? $"/dbins/" : $"/";
            ftp상태.현재FTP경로 = 기본FTP경로;

            while (true)
            {
                try
                {
                    long 전체전송누적 = 0;
                    long 전체업로드크기 = 0;

                    List<레코드> 레코드목록 = new List<레코드>();

                    if (상태.처리단계 != "전체약관")
                    {
                        logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [07-B] 개별 파일 업로드 준비", 1);

                        레코드목록 = 레코드s.Where(x => (x.발송구분 == "02" || x.발송구분 == "2") && x.개별PDF작업 == "개별PDF완료" && x.FTP작업 != "FTP완료").OrderBy(x => x.고객번호).ToList();
                        if (레코드목록.Count == 0) return "실패";

                        string 전송대상폴더 = $"{상태.출력폴더}@맞춤증권\\M\\";
                        Dictionary<string, string> 전송파일 = new Dictionary<string, string>();

                        var files = new DirectoryInfo(전송대상폴더).GetFiles("M*.pdf");

                        foreach (FileInfo 파일 in files)
                        {
                            string tmp전송파일 = System.IO.Path.GetFileNameWithoutExtension(파일.Name);
                            string[] tmp분리 = tmp전송파일.Split('_');
                            if (tmp분리.Length > 2)
                            {
                                string tmp고객번호 = string.Join("_", tmp분리, 2, tmp분리.Length - 2);
                                if (!전송파일.ContainsKey(tmp고객번호)) 전송파일.Add(tmp고객번호, 파일.FullName);
                            }
                        }

                        foreach (var item in 레코드목록)
                        {
                            if (전송파일.TryGetValue(item.고객번호, out string tmpFilePath)) item.모바일파일 = tmpFilePath;
                        }

                        string 로컬전체경로 = 상태.출력폴더;
                        int 시작인덱스 = 로컬전체경로.IndexOf(상태.고객사);
                        string 상대경로 = 로컬전체경로.Substring(시작인덱스);
                        string[] 폴더단계 = 상대경로.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

                        string 현재체크경로 = 기본FTP경로;

                        using (var sftp = new SftpClient(ftp상태.IP, ftp상태.PORT, ftp상태.ID, ftp상태.PW))
                        {
                            sftp.Connect();

                            foreach (string 폴더명 in 폴더단계)
                            {
                                현재체크경로 += 폴더명 + "/";
                                await FTP_폴더체크및생성2(sftp, 현재체크경로);
                            }

                            현재체크경로 += "M/";
                            await FTP_폴더체크및생성2(sftp, 현재체크경로);
                            sftp.Disconnect();
                        }

                        ftp상태.현재FTP경로 = 현재체크경로;
                    }
                    else
                    {
                        레코드목록 = 레코드s.ToList();
                    }

                    전체업로드크기 = 레코드목록
                        .Where(r => !string.IsNullOrWhiteSpace(r.모바일파일) && File.Exists(r.모바일파일))
                        .Sum(r => new FileInfo(r.모바일파일).Length);

                    int 최대동시작업수 = Math.Max(10, 상태.앱실행수);
                    var semaphore = new SemaphoreSlim(최대동시작업수);
                    var sftpPool = new System.Collections.Concurrent.ConcurrentQueue<SftpClient>();
                    for (int i = 0; i < 최대동시작업수; i++)
                    {
                        var client = new SftpClient(ftp상태.IP, ftp상태.PORT, ftp상태.ID, ftp상태.PW);
                        client.Connect();
                        sftpPool.Enqueue(client);
                    }

                    var tasks = new List<Task>();
                    var 완료고객번호 = new ConcurrentBag<string>();
                    var 오류고객번호 = new ConcurrentBag<string>();
                    HashSet<string> 생성된폴더목록 = new HashSet<string>();

                    foreach (var record in 레코드목록)
                    {
                        await semaphore.WaitAsync();

                        var task = Task.Run(async () =>
                        {
                            SftpClient 현재스레드용sftp = null;
                            try
                            {
                                sftpPool.TryDequeue(out 현재스레드용sftp);

                                // 통약관 대상 여부 확인
                                bool 통약관전환대상 = (상태.처리단계 != "전체약관") && string.IsNullOrWhiteSpace(record.전체약관경로) && (record.담보코드 == "*");

                                // 0. 이미 전체약관 경로가 세팅되어 있다면 업로드 패스
                                if (!string.IsNullOrWhiteSpace(record.전체약관경로))
                                {
                                    실행여부 = "성공";
                                }
                                // =========================================================
                                // [분기 1] 전체약관 또는 통약관 전환 대상 -> YagGwan 폴더로 업로드
                                // =========================================================
                                else if ((상태.처리단계 == "전체약관" || 통약관전환대상) && 고객사 == "KB2")
                                {
                                    string[] 폴더단계 = new string[] { record.고객사, "YagGwan", record.상품코드 };
                                    string 현재체크경로 = 기본FTP경로;

                                    // 순차적 폴더 생성 로직 (정상 작동)
                                    foreach (string 폴더명 in 폴더단계)
                                    {
                                        현재체크경로 = $"{현재체크경로}{폴더명}/";
                                        bool 폴더작업필요 = false;

                                        lock (생성된폴더목록)
                                        {
                                            if (!생성된폴더목록.Contains(현재체크경로))
                                            {
                                                폴더작업필요 = true;
                                                생성된폴더목록.Add(현재체크경로);
                                            }
                                        }

                                        if (폴더작업필요)
                                        {
                                            await FTP_폴더체크및생성2(현재스레드용sftp, 현재체크경로);
                                        }
                                    }

                                    string 통약관FTP폴더 = 현재체크경로;

                                    // ★ 질문2 반영: is통약관 파라미터에 명시적으로 true 전달
                                    실행여부 = await 실행(() => FTP_파일업로드2(현재스레드용sftp, record, 통약관FTP폴더, (전송누적, 파일크기, 방금전송된량) =>
                                    {
                                        Interlocked.Add(ref 전체전송누적, 방금전송된량);
                                        int 현재진행 = (int)((double)전송누적 / 파일크기 * 100);
                                        if (현재진행 > 100) 현재진행 = 100;
                                        long 전체현재 = Interlocked.Read(ref 전체전송누적);
                                        int 전체진행 = (int)((double)전체현재 / 전체업로드크기 * 100);
                                        if (전체진행 > 100) 전체진행 = 100;
                                        string 표시진행 = $"{전체현재 / 1024.0 / 1024.0:00000.0}/{전체업로드크기 / 1024.0 / 1024.0:00000.0} MB";
                                        progressCallback?.Invoke(new string[] { "FTP진행", 현재진행.ToString(), 전체진행.ToString(), 표시진행 });
                                    }, logCallback, true), "FTP_파일업로드2_통약관", logCallback); // <== true 전달
                                }
                                // =========================================================
                                // [분기 2] 순수 맞춤약관 -> M 폴더로 업로드
                                // =========================================================
                                else
                                {
                                    string 맞춤약관FTP폴더 = ftp상태.현재FTP경로; // M폴더는 루프 밖에서 이미 생성됨

                                    // ★ is통약관 = false 파라미터 전달 (기본값이지만 명확하게 명시)
                                    실행여부 = await 실행(() => FTP_파일업로드2(현재스레드용sftp, record, 맞춤약관FTP폴더, (전송누적, 파일크기, 방금전송된량) =>
                                    {
                                        Interlocked.Add(ref 전체전송누적, 방금전송된량);
                                        int 현재진행 = (int)((double)전송누적 / 파일크기 * 100);
                                        if (현재진행 > 100) 현재진행 = 100;
                                        long 전체현재 = Interlocked.Read(ref 전체전송누적);
                                        int 전체진행 = (int)((double)전체현재 / 전체업로드크기 * 100);
                                        if (전체진행 > 100) 전체진행 = 100;
                                        string 표시진행 = $"{전체현재 / 1024.0 / 1024.0:00000.0}/{전체업로드크기 / 1024.0 / 1024.0:00000.0} MB";
                                        progressCallback?.Invoke(new string[] { "FTP진행", 현재진행.ToString(), 전체진행.ToString(), 표시진행 });
                                    }, logCallback, false), "FTP_파일업로드2_맞춤", logCallback); // <== false 전달
                                    
                                }

                                if (실행여부 == "성공")
                                {
                                    완료고객번호.Add(record.고객번호);
                                    record.FTP작업 = "FTP완료";
                                }
                                else
                                {
                                    오류고객번호.Add(record.고객번호);
                                    record.FTP작업 = "FTP오류";
                                }
                            }
                            catch (Exception)
                            {
                                오류고객번호.Add(record.고객번호);
                                throw;
                            }
                            finally
                            {
                                if (현재스레드용sftp != null)
                                {
                                    sftpPool.Enqueue(현재스레드용sftp);
                                }
                                semaphore.Release();
                            }
                        });
                        tasks.Add(task);
                    }

                    await Task.WhenAll(tasks);

                    while (sftpPool.TryDequeue(out var client))
                    {
                        client.Disconnect();
                        client.Dispose();
                    }

                    if (상태.처리단계 != "전체약관")
                    {
                        if (await 실행(() => BatchUpdateAsync(상태, 완료고객번호, 오류고객번호, "FTP", logCallback), "FTP_FILE_UPLOAD2(BatchUpdateAsync)", logCallback) == "실패")
                        {
                            throw new Exception("FTP_FILE_UPLOAD2(BatchUpdateAsync) 오류, 로그참조");
                        }
                    }
                    break;
                }
                catch (Exception)
                {
                    실행여부 = "실패";
                    throw;
                }
            }
            return 실행여부;
        }

        // ==========================================================================================================
        // 2. 일반 FTP 업로드
        // ==========================================================================================================
        public static async Task<string> FTP_FILE_UPLOAD(
            현재상태 상태,
            List<레코드> 레코드s,
            Action<string, int> logCallback,
            Action<string[]> progressCallback)
        {
            string 실행여부 = "성공";
            System.Net.ServicePointManager.DefaultConnectionLimit = 50;

            string tmpFTP설정값 = "";
            if (!저장.설정값.TryGetValue($"FTP{고객사선택(상태.고객사, 저장.사용컴퓨터)}", out tmpFTP설정값))
            {
                throw new Exception("FTP설정값 미존재, FTP전송 중단");
            }
            string[] 계정 = tmpFTP설정값.Split('|').Select(P => P.Trim()).ToArray();

            FTP상태 ftp상태 = new FTP상태();
            if (계정.Length == 4)
                ftp상태 = new FTP상태 { IP = 계정[0], PORT = Convert.ToInt32(계정[1]), ID = 계정[2], PW = 계정[3] };
            else
                ftp상태 = new FTP상태 { IP = 계정[0], ID = 계정[1], PW = 계정[2] };

            string 기본FTP경로 = 상태.고객사 == "DB" ? $"ftp://{ftp상태.IP}/dbins/" : $"ftp://{ftp상태.IP}/";
            ftp상태.현재FTP경로 = 기본FTP경로;

            while (true)
            {
                try
                {
                    long 전체전송누적 = 0;
                    long 전체업로드크기 = 0;
                    List<레코드> 레코드목록 = new List<레코드>();

                    if (상태.처리단계 != "전체약관")
                    {
                        logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [07-B] 개별 파일 업로드 준비", 1);

                        레코드목록 = 레코드s.Where(x => (x.발송구분 == "02" || x.발송구분 == "2") && x.개별PDF작업 == "개별PDF완료" && x.FTP작업 != "FTP완료").OrderBy(x => x.고객번호).ToList();
                        if (레코드목록.Count == 0) return "실패";

                        string 전송대상폴더 = $"{상태.출력폴더}@맞춤증권\\M\\";
                        Dictionary<string, string> 전송파일 = new Dictionary<string, string>();

                        var files = new DirectoryInfo(전송대상폴더).GetFiles("M*.pdf");
                        if (files.Length == 0) throw new Exception("M폴더에 파일 미존재, FTP전송 중단");

                        foreach (FileInfo 파일 in files)
                        {
                            string tmp전송파일 = System.IO.Path.GetFileNameWithoutExtension(파일.Name);
                            string[] tmp분리 = tmp전송파일.Split('_');
                            if (tmp분리.Length > 2)
                            {
                                string tmp고객번호 = string.Join("_", tmp분리, 2, tmp분리.Length - 2);
                                if (!전송파일.ContainsKey(tmp고객번호)) 전송파일.Add(tmp고객번호, 파일.FullName);
                            }
                        }

                        foreach (var item in 레코드목록)
                        {
                            if (전송파일.TryGetValue(item.고객번호, out string tmpFilePath)) item.모바일파일 = tmpFilePath;
                        }

                        string 로컬전체경로 = 상태.출력폴더;
                        int 시작인덱스 = 로컬전체경로.IndexOf(상태.고객사);
                        string 상대경로 = 로컬전체경로.Substring(시작인덱스);
                        string[] 폴더단계 = 상대경로.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

                        string 현재체크경로 = 기본FTP경로;
                        foreach (string 폴더명 in 폴더단계)
                        {
                            현재체크경로 += 폴더명 + "/";
                            await FTP_폴더체크및생성(ftp상태, 현재체크경로);
                        }

                        현재체크경로 += "M/";
                        await FTP_폴더체크및생성(ftp상태, 현재체크경로);

                        ftp상태.현재FTP경로 = 현재체크경로;
                    }
                    else
                    {
                        레코드목록 = 레코드s.ToList();
                    }

                    전체업로드크기 = 레코드목록.Sum(r => new FileInfo(r.모바일파일).Length);

                    var semaphore = new SemaphoreSlim(Math.Max(5, 상태.앱실행수));
                    var tasks = new List<Task>();
                    var 완료고객번호 = new ConcurrentBag<string>();
                    var 오류고객번호 = new ConcurrentBag<string>();
                    HashSet<string> 생성된폴더목록 = new HashSet<string>();

                    foreach (var record in 레코드목록)
                    {
                        await semaphore.WaitAsync();

                        var task = Task.Run(async () =>
                        {
                            try
                            {
                                string 맞춤약관FTP폴더 = ftp상태.현재FTP경로;
                                string 통약관FTP폴더 = "";

                                bool 통약관전환대상 = (상태.처리단계 != "전체약관") && string.IsNullOrWhiteSpace(record.전체약관경로) && (record.담보코드 == "*");

                                if (상태.처리단계 == "전체약관" || 통약관전환대상)
                                {
                                    string[] 폴더단계 = new string[] { record.고객사, "YagGwan", record.상품코드 };
                                    string 현재체크경로 = 기본FTP경로;

                                    foreach (string 폴더명 in 폴더단계)
                                    {
                                        현재체크경로 = $"{현재체크경로}{폴더명}/";
                                        bool 폴더작업필요 = false;
                                        lock (생성된폴더목록)
                                        {
                                            if (!생성된폴더목록.Contains(현재체크경로))
                                            {
                                                폴더작업필요 = true;
                                                생성된폴더목록.Add(현재체크경로);
                                            }
                                        }

                                        if (폴더작업필요)
                                        {
                                            await FTP_폴더체크및생성(ftp상태, 현재체크경로);
                                        }
                                    }
                                    통약관FTP폴더 = 현재체크경로;
                                }

                                if (상태.처리단계 == "전체약관")
                                {
                                    if (!string.IsNullOrWhiteSpace(record.전체약관경로))
                                    {
                                        실행여부 = "성공";
                                    }
                                    else
                                    {
                                        실행여부 = await 실행(() => FTP_파일업로드(ftp상태, record, 통약관FTP폴더, (전송누적, 파일크기, 방금전송된량) =>
                                        {
                                            Interlocked.Add(ref 전체전송누적, 방금전송된량);

                                            int 현재진행 = (int)((double)전송누적 / 파일크기 * 100);
                                            if (현재진행 > 100) 현재진행 = 100;

                                            long 전체현재 = Interlocked.Read(ref 전체전송누적);
                                            int 전체진행 = (int)((double)전체현재 / 전체업로드크기 * 100);
                                            if (전체진행 > 100) 전체진행 = 100;

                                            string 표시진행 = $"{전체현재 / 1024.0 / 1024.0:00000.0}/{전체업로드크기 / 1024.0 / 1024.0:00000.0} MB";

                                            progressCallback?.Invoke(new string[] { "FTP진행", 현재진행.ToString(), 전체진행.ToString(), 표시진행 });

                                        }, logCallback), "FTP_파일업로드", logCallback);
                                    }
                                }
                                else
                                {
                                    if (!string.IsNullOrWhiteSpace(record.전체약관경로))
                                    {
                                        실행여부 = "성공";
                                    }
                                    else
                                    {
                                        실행여부 = await 실행(() => FTP_파일업로드(ftp상태, record, 맞춤약관FTP폴더, (전송누적, 파일크기, 방금전송된량) =>
                                        {
                                            Interlocked.Add(ref 전체전송누적, 방금전송된량);

                                            int 현재진행 = (int)((double)전송누적 / 파일크기 * 100);
                                            if (현재진행 > 100) 현재진행 = 100;

                                            long 전체현재 = Interlocked.Read(ref 전체전송누적);
                                            int 전체진행 = (int)((double)전체현재 / 전체업로드크기 * 100);
                                            if (전체진행 > 100) 전체진행 = 100;

                                            string 표시진행 = $"{전체현재 / 1024.0 / 1024.0:00000.0}/{전체업로드크기 / 1024.0 / 1024.0:00000.0} MB";

                                            progressCallback?.Invoke(new string[] { "FTP진행", 현재진행.ToString(), 전체진행.ToString(), 표시진행 });

                                        }, logCallback), "FTP_파일업로드", logCallback);

                                        if (실행여부 == "성공" && 통약관전환대상)
                                        {
                                            string 백업결과 = await 실행(() => FTP_파일업로드(ftp상태, record, 통약관FTP폴더, (a, b, c) => { }, logCallback), "FTP_파일업로드_통약관백업", logCallback);

                                            if (백업결과 != "성공")
                                            {
                                                logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [경고] 맞춤약관 완료, 통약관 백업 실패: {record.고객번호}", 1);
                                            }
                                        }
                                    }
                                }

                                if (실행여부 == "성공")
                                {
                                    완료고객번호.Add(record.고객번호);
                                    record.FTP작업 = "FTP완료";
                                }
                                else
                                {
                                    오류고객번호.Add(record.고객번호);
                                    record.FTP작업 = "FTP오류";
                                }
                            }
                            catch (Exception)
                            {
                                오류고객번호.Add(record.고객번호);
                                throw;
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        });
                        tasks.Add(task);
                    }

                    await Task.WhenAll(tasks);

                    if (상태.처리단계 != "전체약관")
                    {
                        if (await 실행(() => BatchUpdateAsync(상태, 완료고객번호, 오류고객번호, "FTP", logCallback), "FTP_FILE_UPLOAD(BatchUpdateAsync)", logCallback) == "실패")
                        {
                            throw new Exception("FTP_FILE_UPLOAD(BatchUpdateAsync) 오류, 로그참조");
                        }
                    }
                    break;
                }
                catch (Exception)
                {
                    실행여부 = "실패";
                    throw;
                }
            }
            return 실행여부;
        }

        // ==========================================================================================================
        // 파일 업로드 코어 함수들 (🔥 핵심 버그 수정 부분)
        // ==========================================================================================================
        public static async Task<string> FTP_폴더체크및생성2(SftpClient sftp, string 타겟경로Url)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!sftp.Exists(타겟경로Url))
                    {
                        sftp.CreateDirectory(타겟경로Url);
                    }
                    return "성공";
                }
                catch (Exception ex)
                {
                    throw new Exception($"SFTP 폴더 체크/생성 실패 (순차): {타겟경로Url} - {ex.Message}");
                }
            });
        }

        public static async Task<string> FTP_파일업로드2(
            SftpClient sftp, // ★ 연결된 클라이언트를 주입받음
            레코드 레코드,
            string 타겟폴더경로,
            Action<long, long, long> progressCallback,
            Action<string, int> logCallback,
            bool is통약관 = false)
        {
            int 재시도 = 0;
            string 실행여부 = "실패";

            if (!string.IsNullOrWhiteSpace(레코드.전체약관경로)) return "성공";
            if (string.IsNullOrEmpty(레코드.모바일파일)) throw new Exception("업로드할 파일 경로가 없습니다.");

            FileInfo localFileInfo = new FileInfo(레코드.모바일파일);
            if (!localFileInfo.Exists) throw new Exception($"로컬 파일이 존재하지 않습니다: {레코드.모바일파일}");

            string 종분류코드 = string.IsNullOrWhiteSpace(레코드.종분류코드) ? "0" : 레코드.종분류코드;
            string 최종파일명 = is통약관
                                ? $"{레코드.상품코드}_{종분류코드}_{레코드.개정일}.pdf"
                                : localFileInfo.Name;

            string 업로드URL = $"{타겟폴더경로}{최종파일명}";
            int lastPercent = -1;

            // ★ 클라이언트 버퍼 사이즈 상향 (최대 전송 속도 향상)
            sftp.BufferSize = (uint)(1024 * 64); // 64KB 이상 추천

            while (재시도 <= 10 && 실행여부 != "성공")
            {
                try
                {
                    // ★ 중요: 여기서 sftp.Connect()를 하지 않습니다. 이미 연결된 것을 사용합니다.
                    using (FileStream fileStream = new FileStream(레코드.모바일파일, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        ulong 이전전송누적 = 0;
                        double 파일크기 = localFileInfo.Length;

                        sftp.UploadFile(fileStream, 업로드URL, (uploadedBytes) =>
                        {
                            int currentPercent = (int)(((double)uploadedBytes / 파일크기) * 100);

                            if (currentPercent > lastPercent || uploadedBytes == (ulong)파일크기)
                            {
                                lastPercent = currentPercent;
                                long 방금전송된량 = (long)uploadedBytes - (long)이전전송누적;
                                이전전송누적 = uploadedBytes;

                                progressCallback?.Invoke((long)uploadedBytes, (long)파일크기, 방금전송된량);
                            }
                        });
                    }

                    실행여부 = "성공";
                    logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [성공] SFTP업로드 완료: {localFileInfo.Name}", 1);
                }
                catch (Exception ex)
                {
                    재시도++;
                    logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [오류] Upload 재시도({재시도}/10) {localFileInfo.Name} 오류: {ex.Message}", 1);

                    // 연결이 끊겼을 경우만 재연결 시도
                    if (!sftp.IsConnected) sftp.Connect();

                    if (재시도 >= 10) break;
                    await Task.Delay(TimeSpan.FromSeconds(1)); // 재시도 간격 단축
                }
            }
            return 실행여부;
        }

        public static async Task<long> SFTP_파일크기체크(FTP상태 ftp상태, string 업로드파일경로)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var sftp = new SftpClient(ftp상태.IP, ftp상태.PORT, ftp상태.ID, ftp상태.PW))
                    {
                        sftp.Connect();
                        if (sftp.Exists(업로드파일경로))
                        {
                            var attributes = sftp.GetAttributes(업로드파일경로);
                            sftp.Disconnect();
                            return attributes.Size;
                        }
                        sftp.Disconnect();
                        return -1;
                    }
                }
                catch
                {
                    return -1;
                }
            });
        }

        public static async Task<string> FTP_폴더체크및생성(FTP상태 ftp상태, string 타겟경로Url)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(타겟경로Url);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = new NetworkCredential(ftp상태.ID, ftp상태.PW);
                request.Timeout = 2000;

                using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                {
                    return "성공";
                }
            }
            catch (WebException)
            {
                return await FTP_폴더생성_실행(ftp상태, 타겟경로Url);
            }
        }

        public static async Task<string> FTP_폴더생성_실행(FTP상태 ftp상태, string 생성할경로Url)
        {
            int 재시도 = 0;
            while (재시도 < 3)
            {
                try
                {
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(생성할경로Url);
                    request.Method = WebRequestMethods.Ftp.MakeDirectory;
                    request.Credentials = new NetworkCredential(ftp상태.ID, ftp상태.PW);
                    request.UseBinary = true;
                    request.KeepAlive = false;

                    using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                    {
                        if (response.StatusCode == FtpStatusCode.PathnameCreated)
                            return "성공";
                    }
                }
                catch (WebException ex)
                {
                    FtpWebResponse response = (FtpWebResponse)ex.Response;
                    if (response != null && response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                    {
                        return "성공";
                    }
                    재시도++;
                    await Task.Delay(1000);
                }
                catch (Exception)
                {
                    재시도++;
                    await Task.Delay(1000);
                }
            }
            throw new Exception($"FTP 폴더 생성 실패: {생성할경로Url}");
        }

        public static async Task<string> FTP_파일업로드(
            FTP상태 ftp상태,
            레코드 레코드,
            string 타겟폴더경로,
            Action<long, long, long> progressCallback,
            Action<string, int> logCallback)
        {
            int 재시도 = 0;
            string 실행여부 = "실패";

            if (string.IsNullOrEmpty(레코드.모바일파일))
                throw new Exception("업로드할 파일 경로가 없습니다.");

            FileInfo localFileInfo = new FileInfo(레코드.모바일파일);
            if (!localFileInfo.Exists)
                throw new Exception($"로컬 파일이 존재하지 않습니다: {레코드.모바일파일}");

            string 업로드URL = $"{타겟폴더경로}{Uri.EscapeDataString(localFileInfo.Name)}";
            int lastPercent = -1;

            while (재시도 <= 10 && 실행여부 != "성공")
            {
                try
                {
                    Uri url = new Uri(업로드URL);
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
                    request.Credentials = new NetworkCredential(ftp상태.ID, ftp상태.PW);
                    request.Method = WebRequestMethods.Ftp.UploadFile;
                    request.UseBinary = true;
                    request.KeepAlive = true;
                    request.Timeout = 60000;
                    request.ServicePoint.ConnectionLimit = 50;
                    request.ContentLength = localFileInfo.Length;

                    int bufferSize = 1024 * 1024 * 2;
                    byte[] buffer = new byte[bufferSize];
                    int readCount;

                    long 전송누적 = 0; // 🔥 double에서 long으로 통일하여 정밀도 유지
                    long 이전전송누적 = 0;
                    double 파일크기 = localFileInfo.Length;

                    using (Stream writeStream = await request.GetRequestStreamAsync())
                    using (FileStream readStream = new FileStream(레코드.모바일파일, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true))
                    {
                        while ((readCount = await readStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await writeStream.WriteAsync(buffer, 0, readCount);

                            전송누적 += readCount;

                            int currentPercent = (int)((전송누적 / 파일크기) * 100);

                            // 🔥 수정: 일반 FTP도 동일한 원리로 델타값 보정
                            if (currentPercent > lastPercent || 전송누적 == localFileInfo.Length)
                            {
                                lastPercent = currentPercent;
                                long 방금전송된량 = 전송누적 - 이전전송누적;
                                이전전송누적 = 전송누적;

                                progressCallback?.Invoke(전송누적, (long)파일크기, 방금전송된량);
                            }
                        }
                    }

                    using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                    {
                        if (response.StatusCode == FtpStatusCode.ClosingData || response.StatusCode == FtpStatusCode.FileActionOK)
                        {
                            실행여부 = "성공";
                            logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [성공] FTP업로드 완료: {localFileInfo.Name}", 1);
                        }
                        else
                        {
                            throw new Exception($"FTP 업로드 실패 응답 코드: {response.StatusCode}");
                        }
                    }

                }
                catch (WebException ex)
                {
                    재시도++;
                    logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [오류] Upload 재시도({재시도}/10) {localFileInfo.Name} 오류: {ex.Message}", 1);

                    try
                    {
                        string localMd5 = await MD5_계산(레코드.모바일파일);
                        string serverMd5 = await FTP_MD5체크(ftp상태, 업로드URL);

                        if (!string.IsNullOrEmpty(serverMd5) && string.Equals(localMd5, serverMd5, StringComparison.OrdinalIgnoreCase))
                        {
                            실행여부 = "성공";
                            logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [복구] MD5 일치함. 성공 간주.", 1);
                            break;
                        }
                    }
                    catch (Exception md5Ex)
                    {
                        logCallback?.Invoke($"[MD5체크 실패] {md5Ex.Message} -> 재시도 진행", 0);
                    }

                    if (재시도 >= 10)
                    {
                        실행여부 = "실패";
                        break;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    재시도++;
                    logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [치명적오류] {localFileInfo.Name} : {ex.Message}", 1);

                    if (재시도 >= 10) break;
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
            return 실행여부;
        }

        public static async Task<string> MD5_계산(string 파일경로)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            using (var stream = File.OpenRead(파일경로))
            {
                byte[] hash = md5.ComputeHash(stream);
                await Task.Delay(TimeSpan.FromSeconds(0.01));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public static async Task<string> FTP_MD5체크(FTP상태 ftp상태, string 업로드파일경로)
        {
            Uri url = new Uri(업로드파일경로);
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
            request.Credentials = new NetworkCredential(ftp상태.ID, ftp상태.PW);
            request.Method = "XMD5";
            request.UseBinary = true;

            using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                string result = await reader.ReadToEndAsync();
                return result.Trim();
            }
        }

        // ===================================================================================================================================
        // 3. 모바일 데이터(DB) 업로드 
        // ====================================================================================================================================

        // NetworkManager.cs 내부에 추가
        public static async Task 약관_사전중복체크(현재상태 상태, List<레코드> 레코드s, Action<string, int> logCallback)
        {
            // 약관 DB 접속 문자열이 비어있는지 안전하게 확인합니다.
            if (string.IsNullOrWhiteSpace(저장.약관DB))
                throw new Exception("약관DB 접속 설정값이 없습니다. 사전체크 중단");

            // 수정됨: 모바일AdoNet이 아닌 약관AdoNet으로 접속
            using (SqlConnection 약관AdoNet = new SqlConnection(저장.약관DB))
            {
                await 약관AdoNet.OpenAsync();

                string checkSql = $"SELECT 1 FROM 약관_{상태.고객사} WHERE 약관PK = @약관PK";

                foreach (var 레코드 in 레코드s)
                {
                    // 통약관 대상인 경우에만 체크
                    bool 통약관전환대상 = (상태.처리단계 != "전체약관") && string.IsNullOrWhiteSpace(레코드.전체약관경로) && (레코드.담보코드 == "*");

                    if (상태.처리단계 == "전체약관" || 통약관전환대상)
                    {
                        string 종분류코드 = 레코드.종분류코드 == "" ? "0" : 레코드.종분류코드;
                        string 현재약관PK = $"{레코드.상품코드}_{종분류코드}_{레코드.개정일}";

                        using (var cmd = new SqlCommand(checkSql, 약관AdoNet))
                        {
                            cmd.Parameters.Add("@약관PK", SqlDbType.NVarChar).Value = 현재약관PK;

                            // 데이터가 있으면 중복으로 간주
                            var result = await cmd.ExecuteScalarAsync();
                            if (result != null)
                            {
                                레코드.중복스킵여부 = true;
                                logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [DB사전검사] 이미 등록된 약관 (약관PK: {현재약관PK})", 1);
                            }
                        }
                    }
                }
            }
        }

        public static async Task<string> 모바일데이터업로드(
            현재상태 상태,
            List<레코드> 레코드s,
            Action<string, int> logCallback)
        {
            int 재시도 = 0;
            string 실행여부 = "성공";

            while (true)
            {
                try
                {
                    string 모바일Tabel = "";
                    string 고객사 = 고객사선택(상태.고객사, 저장.사용컴퓨터) ?? "";
                    if (!저장.설정값.TryGetValue($"모바일{고객사}", out 모바일Tabel))
                        throw new Exception("모바일Table설정값 미존재, Table 업로드 중단");

                    int 시작 = 상태.출력폴더.IndexOf(상태.고객사);
                    string 입력파일명 = "";


                    // ★ [핵심1] 업로드 경로를 통약관용과 맞춤약관용으로 명확히 분리합니다.
                    string 통약관_업로드경로 = "";
                    string 맞춤약관_업로드경로 = "";


                    if (상태.고객사 == "DB")
                    {
                        int 고객사위치 = 상태.groupKey.IndexOf(string.Format("#{0}", 상태.고객사)) + 4;
                        입력파일명 = 상태.groupKey.Substring(고객사위치);
                        
                        맞춤약관_업로드경로 = string.Format(@"z:\{0}M\", 상태.출력폴더.Substring(시작)).Replace(@"\", @"/");
                        통약관_업로드경로 = 맞춤약관_업로드경로; // 추후 통약관 사용을 하면 수정할것 (지금은 사용않함)
                    }
                    else
                    {
                        string 기본경로 = (상태.처리단계 == "전체약관" || 고객사 == "KB2") ? @"f:\upload\pdf\" : @"d:\upload\pdf\";

                        통약관_업로드경로 = $@"{기본경로}{상태.고객사}\YagGwan\";
                        맞춤약관_업로드경로 = $@"{기본경로}{상태.출력폴더.Substring(시작)}M\";
                    }

                    List<레코드> 레코드목록 = new List<레코드>();
                    if (상태.처리단계 == "전체약관")
                    {
                        레코드목록 = 레코드s.ToList();
                    }
                    else
                    {
                        string 모바일폴더 = string.Format("{0}@맞춤증권\\M\\", 상태.출력폴더);
                        Dictionary<string, string> 모바일파일 = new Dictionary<string, string>();
                        foreach (FileInfo 파일 in new DirectoryInfo(모바일폴더).GetFiles("M*.pdf"))
                        {
                            string tmp모바일파일 = System.IO.Path.GetFileNameWithoutExtension(파일.Name);
                            string[] tmp분리 = tmp모바일파일.Split('_');
                            string tmp고객번호 = string.Join("_", tmp분리, 2, tmp분리.Length - 2);
                            모바일파일.Add(tmp고객번호, 파일.FullName);
                        }

                        foreach (var item in 레코드s)
                        {
                            if (모바일파일.TryGetValue(item.고객번호, out string tmp모바일파일))
                                item.모바일파일 = tmp모바일파일;
                        }

                        레코드목록 = 레코드s
                            .Where(x => (x.발송구분 == "02" || x.발송구분 == "2") &&
                                        x.FTP작업 == "FTP완료" &&
                                        x.모바일작업 != "모바일완료")
                            .OrderBy(x => x.고객번호)
                            .ToList();
                    }

                    var 오류고객번호 = new ConcurrentBag<string>();
                    var 완료고객번호 = new ConcurrentBag<string>();
                    var tmp완료오류 = new ConcurrentBag<string>();
                                        
                    List<string> 맞춤약관URL = new List<string>();
                    List<string> 통약관URL = new List<string>();
                    Dictionary<string, string> KBUrl고객 = new Dictionary<string, string>();

                    SqlConnection 모바일AdoNet = new SqlConnection(모바일Tabel);
                    bool 약관DB필요 = 상태.처리단계 == "전체약관" || 레코드목록.Any(r => string.IsNullOrWhiteSpace(r.전체약관경로) && r.담보코드 == "*");
                    SqlConnection 약관AdoNet = 약관DB필요 ? new SqlConnection(저장.약관DB) : null;

                    try
                    {
                        await 모바일AdoNet.OpenAsync();
                        if (약관AdoNet != null) await 약관AdoNet.OpenAsync();

                        using (var 모바일Transaction = 모바일AdoNet.BeginTransaction())
                        using (var 약관Transaction = 약관AdoNet?.BeginTransaction())
                        {
                            try
                            {
                                string insertSql = @"
                                    INSERT INTO CSTM_INFO (
                                        [KEY], CMP_CD, ID, FILE_PATH, META, FLAG, USE_YN, CRT_DT, UDT_DT,
                                        " + (상태.고객사 == "DB" ? "TRANS_ID," : "") + @" CH
                                    )
                                    VALUES (
                                        @KEY, @CMP_CD, @ID, @FILE_PATH, @META, @FLAG, @USE_YN, @CRT_DT, @UDT_DT,
                                        " + (상태.고객사 == "DB" ? "@TRANS_ID," : "") + @" @CH
                                    )";

                                string insertYagGwanSql = $@"
                                    IF NOT EXISTS (SELECT 1 FROM 약관_{상태.고객사} WHERE 약관PK = @약관PK)
                                    BEGIN
                                        INSERT INTO 약관_{상태.고객사} (
                                            약관PK, 상품명, 상품코드, 개정일, 담보분류, 작성일, 파일경로, URL
                                        )
                                        VALUES (
                                           @약관PK, @상품명, @상품코드, @개정일, @담보분류, @작성일, @파일경로, @URL
                                        )
                                    END";

                                int 중복방지cnt = 0;

                                foreach (var 레코드 in 레코드목록)
                                {
                                    if (레코드.모바일파일 == "" && string.IsNullOrWhiteSpace(레코드.전체약관경로))
                                    {
                                        오류고객번호.Add(레코드.고객번호);
                                        continue;
                                    }
                                    tmp완료오류.Add(레코드.고객번호);

                                    bool 통약관전환대상 = (상태.처리단계 != "전체약관") && string.IsNullOrWhiteSpace(레코드.전체약관경로) && (레코드.담보코드 == "*") && 고객사 == "KB2";

                                    // =============================================================================================
                                    // [1] 통약관 DB 등록 
                                    // =============================================================================================
                                    if (상태.처리단계 == "전체약관" || 통약관전환대상)
                                    {
                                        중복방지cnt++;
                                        string 통약관중복방지 = $"{DateTime.Now:yyMMddHHmmss}{중복방지cnt:000000}";
                                        string 통약관임시키 = $"{레코드.상품코드}{레코드.개정일}";
                                        string 통약관TmpKey = FnGetKeyCSharp(레코드.고객사, $"{통약관임시키}{통약관중복방지}");

                                        string 종분류코드 = 레코드.종분류코드 == "" ? "0" : 레코드.종분류코드;
                                        string 통약관파일명 = $"{레코드.상품코드}_{종분류코드}_{레코드.개정일}.pdf";
                                        
                                        string 통약관최종경로 = $@"{통약관_업로드경로}{레코드.상품코드}\{통약관파일명}";

                                        // 1-A. CSTM_INFO INSERT
                                        using (var cmd = new SqlCommand(insertSql, 모바일AdoNet, 모바일Transaction))
                                        {
                                            cmd.Parameters.Add("@KEY", SqlDbType.NVarChar).Value = 통약관TmpKey;
                                            cmd.Parameters.Add("@CMP_CD", SqlDbType.NVarChar).Value = 레코드.고객사;
                                            cmd.Parameters.Add("@ID", SqlDbType.NVarChar).Value = 통약관임시키;
                                            cmd.Parameters.Add("@FILE_PATH", SqlDbType.NVarChar).Value = 통약관최종경로;
                                            cmd.Parameters.Add("@META", SqlDbType.NVarChar).Value = "";
                                            cmd.Parameters.Add("@FLAG", SqlDbType.NVarChar).Value = "";
                                            cmd.Parameters.Add("@USE_YN", SqlDbType.NVarChar).Value = "Y";
                                            cmd.Parameters.Add("@CRT_DT", SqlDbType.DateTime).Value = DateTime.Now;
                                            cmd.Parameters.Add("@UDT_DT", SqlDbType.DateTime).Value = DateTime.Now;
                                            if (상태.고객사 == "DB") cmd.Parameters.Add("@TRANS_ID", SqlDbType.NVarChar).Value = 입력파일명;
                                            cmd.Parameters.Add("@CH", SqlDbType.NVarChar).Value = "약관";
                                            int 적용건수 = await cmd.ExecuteNonQueryAsync();
                                            logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [DBUpdate로그] 통약관 CSTM_INFO 등록 완료: {적용건수}건 (약관파일 : {통약관파일명})", 1);
                                        }

                                        string 기본URL = @"https://kbinsure2.datazone.co.kr/directDownload.pdf?key=";

                                        // 1-B. 약관 DB INSERT
                                        if (약관AdoNet != null)
                                        {
                                            using (var cmdYagGwan = new SqlCommand(insertYagGwanSql, 약관AdoNet, 약관Transaction))
                                            {
                                                cmdYagGwan.Parameters.Add("@약관PK", SqlDbType.NVarChar).Value = $"{레코드.상품코드}_{종분류코드}_{레코드.개정일}";
                                                cmdYagGwan.Parameters.Add("@상품명", SqlDbType.NVarChar).Value = 레코드.상품명;
                                                cmdYagGwan.Parameters.Add("@상품코드", SqlDbType.NVarChar).Value = 레코드.상품코드;
                                                cmdYagGwan.Parameters.Add("@개정일", SqlDbType.NVarChar).Value = 레코드.개정일;
                                                cmdYagGwan.Parameters.Add("@담보분류", SqlDbType.NVarChar).Value = 종분류코드;
                                                cmdYagGwan.Parameters.Add("@작성일", SqlDbType.NVarChar).Value = $"{DateTime.Now:yyMMddHHmmss}";
                                                cmdYagGwan.Parameters.Add("@파일경로", SqlDbType.NVarChar).Value = 통약관최종경로;
                                                cmdYagGwan.Parameters.Add("@URL", SqlDbType.NVarChar).Value = $"{기본URL}{통약관TmpKey}";
                                                int 적용건수 = await cmdYagGwan.ExecuteNonQueryAsync();

                                                if (적용건수 <= 0)
                                                {
                                                    레코드.중복스킵여부 = true;
                                                    logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [DB안내] 이미 등록된 통약관 스킵됨 (상품코드: {레코드.상품코드}_{레코드.종분류코드}_{레코드.개정일})", 1);
                                                }
                                                else {
                                                    레코드.중복스킵여부 = false;
                                                    logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [DBUpdate로그] 통약관 약관Table 등록 완료: {적용건수}건 (상품코드: {레코드.상품코드}_{레코드.종분류코드}_{레코드.개정일})", 1);
                                                }

                                                    
                                            }
                                        }

                                        // 1-C. 통약관 전용 URL 수집
                                        if ((레코드.고객사 == "KB" && 저장.사용컴퓨터 == "kbmain") || 레코드.고객사 == "DZ")
                                        {
                                            통약관URL.Add($"{레코드.상품명}|{레코드.상품코드}_{종분류코드}_{레코드.개정일}|{기본URL}{통약관TmpKey}");
                                        }
                                    }

                                    // =============================================================================================
                                    // [2] 맞춤약관 DB 등록
                                    // =============================================================================================
                                    if (상태.처리단계 != "전체약관")
                                    {
                                        중복방지cnt++;
                                        string 맞춤중복방지 = $"{DateTime.Now:yyMMddHHmmss}{중복방지cnt:000000}";
                                        string 맞춤임시키 = 레코드.고객번호 ?? "";
                                        string 맞춤TmpKey = FnGetKeyCSharp(레코드.고객사, $"{맞춤임시키}{맞춤중복방지}");

                                        string 맞춤최종경로 = "";
                                        if (!string.IsNullOrWhiteSpace(레코드.전체약관경로))
                                        {
                                            맞춤최종경로 = 레코드.전체약관경로;
                                        }
                                        else if (통약관전환대상)
                                        {
                                            // FTP가 YagGwan에 올렸으므로 DB 경로도 YagGwan으로 고정!
                                            string 종분류코드 = 레코드.종분류코드 == "" ? "0" : 레코드.종분류코드;
                                            string 통약관파일명 = $"{레코드.상품코드}_{종분류코드}_{레코드.개정일}.pdf";
                                            맞춤최종경로 = $@"{통약관_업로드경로}{레코드.상품코드}\{통약관파일명}";
                                        }
                                        else
                                        {
                                            // 순수 맞춤약관일 때만 M 폴더 경로 사용
                                            맞춤최종경로 = $@"{맞춤약관_업로드경로}{System.IO.Path.GetFileName(레코드.모바일파일)}";
                                        }

                                        // 2-A. CSTM_INFO INSERT
                                        using (var cmd = new SqlCommand(insertSql, 모바일AdoNet, 모바일Transaction))
                                        {
                                            cmd.Parameters.Add("@KEY", SqlDbType.NVarChar).Value = 맞춤TmpKey;
                                            cmd.Parameters.Add("@CMP_CD", SqlDbType.NVarChar).Value = 레코드.고객사;
                                            cmd.Parameters.Add("@ID", SqlDbType.NVarChar).Value = 맞춤임시키;
                                            cmd.Parameters.Add("@FILE_PATH", SqlDbType.NVarChar).Value = 맞춤최종경로;
                                            cmd.Parameters.Add("@META", SqlDbType.NVarChar).Value = "";
                                            cmd.Parameters.Add("@FLAG", SqlDbType.NVarChar).Value = "";
                                            cmd.Parameters.Add("@USE_YN", SqlDbType.NVarChar).Value = "Y";
                                            cmd.Parameters.Add("@CRT_DT", SqlDbType.DateTime).Value = DateTime.Now;
                                            cmd.Parameters.Add("@UDT_DT", SqlDbType.DateTime).Value = DateTime.Now;
                                            if (상태.고객사 == "DB") cmd.Parameters.Add("@TRANS_ID", SqlDbType.NVarChar).Value = 입력파일명;
                                            cmd.Parameters.Add("@CH", SqlDbType.NVarChar).Value = 레코드.tmp02;
                                            int 적용건수 = await cmd.ExecuteNonQueryAsync();
                                            logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [DB로그] 맞춤약관 CSTM_INFO 등록 완료: {적용건수}건 (고객번호: {레코드.고객번호})", 1);
                                        }

                                        // 2-B. 맞춤약관 전용 URL 수집 및 알림톡 발송
                                        // 현재 KB장기 모바일은 발송이 에이전트를 KB에서 함
                                        // TM증권만 DZ에서 함
                                        if ((레코드.고객사 == "KB" || 저장.사용컴퓨터 == "kbmain") || 레코드.고객사 == "DZ")
                                        {
                                            string 기본URL1 = @"http://kbinsure.datazone.co.kr/pdfViewer.do?key=";
                                            string 기본URL2 = @"https://kbinsure2.datazone.co.kr/pdfViewer.do?key=";

                                            string 발송URL = "";
                                            if (고객사 == "KB2") {

                                                발송URL = $"{기본URL2}{맞춤TmpKey}";
                                                맞춤약관URL.Add($"{레코드.고객번호}|{발송URL}");
                                                KBUrl고객.Add(레코드.고객번호, 발송URL);
                                            } else
                                            {
                                                발송URL = $"{기본URL1}{맞춤TmpKey}";
                                            }


                                            if (저장.사용컴퓨터 == "main" && 레코드.고객사 == "KB")
                                            {
                                                string tmp고객명 = 레코드.고객명;
                                                if (tmp고객명.Length >= 3) tmp고객명 = $"{tmp고객명.Substring(0, 1)}*{tmp고객명.Substring(2)}";
                                                else if (tmp고객명.Length == 2) tmp고객명 = $"{tmp고객명.Substring(0, 1)}*";

                                                string 알림톡문구 = "[KB손해보험] 모바일 보험 약관 안내\n\n" +
                                                                    $"{tmp고객명} 고객님\n\n" +
                                                                    "고객님께서 체결하신 보험상품에 대한 모바일 약관이 도착했습니다.\n\n" +
                                                                    "▷ 안내문명 : 보험약관\n\n";

                                                string JsonURL = "{\"button\" :[{\"name\" : \"약관 확인\", \"type\" : \"WL\", \"url_mobile\" : \"" + 발송URL + "\", \"target\":\"out\"}]}";

                                                string insertMzSql = @"
                                                    INSERT INTO MZSENDTRAN (
                                                        SN, SENDER_KEY, CHANNEL, SND_TYPE, PHONE_NUM, TMPL_CD,
                                                        SND_MSG, REQ_DTM, SMS_SND_NUM, SMS_SND_YN, ATTACHMENT, SMS_SND_MSG
                                                    )
                                                    VALUES (
                                                        @SN, @SENDER_KEY, @CHANNEL, @SND_TYPE, @PHONE_NUM, @TMPL_CD,
                                                        @SND_MSG, @REQ_DTM, @SMS_SND_NUM, @SMS_SND_YN, @ATTACHMENT, @SMS_SND_MSG
                                                    )";

                                                using (var cmd = new SqlCommand(insertMzSql, 모바일AdoNet, 모바일Transaction))
                                                {
                                                    cmd.Parameters.Add("@SN", SqlDbType.NVarChar).Value = 맞춤TmpKey;
                                                    cmd.Parameters.Add("@SENDER_KEY", SqlDbType.NVarChar).Value = "f11c93a1f1ec2f21aabb1a06a0c782ab633f0699";
                                                    cmd.Parameters.Add("@CHANNEL", SqlDbType.NVarChar).Value = "A";
                                                    cmd.Parameters.Add("@SND_TYPE", SqlDbType.NVarChar).Value = "P";
                                                    cmd.Parameters.Add("@PHONE_NUM", SqlDbType.NVarChar).Value = 레코드.전화번호.Replace("-", "");
                                                    cmd.Parameters.Add("@TMPL_CD", SqlDbType.NVarChar).Value = "kb_Mobi04";
                                                    cmd.Parameters.Add("@SND_MSG", SqlDbType.NVarChar).Value = 알림톡문구;
                                                    cmd.Parameters.Add("@REQ_DTM", SqlDbType.NVarChar).Value = $"{DateTime.Now:yyyyMMdd}140000";
                                                    cmd.Parameters.Add("@SMS_SND_NUM", SqlDbType.NVarChar).Value = "1544-0114";
                                                    cmd.Parameters.Add("@SMS_SND_YN", SqlDbType.NVarChar).Value = "Y";
                                                    cmd.Parameters.Add("@ATTACHMENT", SqlDbType.NVarChar).Value = JsonURL;
                                                    cmd.Parameters.Add("@SMS_SND_MSG", SqlDbType.NVarChar).Value = 알림톡문구;
                                                    int 적용건수 = await cmd.ExecuteNonQueryAsync();
                                                    logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [DB로그] 알림톡(MZSENDTRAN) 등록 완료: {적용건수}건 (고객번호: {레코드.고객번호})", 1);
                                                }
                                            }
                                        }
                                    }
                                }

                                모바일Transaction.Commit();
                                약관Transaction?.Commit();

                                // ★ 핵심 수정: 텍스트 파일 저장 로직 분리
                                if (고객사 == "KB2" || 상태.고객사 == "DZ")
                                {
                                    // 1. 맞춤약관 텍스트 파일 생성 (기존 폴더)
                                    if (맞춤약관URL.Count > 0)
                                    {
                                        string URL저장경로 = $"{저장.기본출력폴더}URL\\{DateTime.Now:yy.MM}\\{DateTime.Now:dd}\\";
                                        Directory.CreateDirectory(URL저장경로);
                                        File.WriteAllLines($"{URL저장경로}{DateTime.Now:yy.MM.dd(HHmmss)}.txt", 맞춤약관URL, Encoding.GetEncoding(949));
                                    }

                                    // 2. 통약관 텍스트 파일 생성 (통약관 전용 폴더)
                                    if (통약관URL.Count > 0)
                                    {
                                        string 통약관URL저장경로 = $"{저장.기본출력폴더}URL(통약관)\\{DateTime.Now:yy.MM}\\{DateTime.Now:dd}\\";
                                        Directory.CreateDirectory(통약관URL저장경로);
                                        // 파일명 끝에 _통약관 을 붙여 식별을 용이하게 합니다.
                                        File.WriteAllLines($"{통약관URL저장경로}{DateTime.Now:yy.MM.dd(HHmmss)}_통약관URL.txt", 통약관URL, Encoding.GetEncoding(949));
                                    }

                                    if (KBUrl고객.Count > 0)
                                        await KBURL반환Table(KBUrl고객, 상태.URLTable접속, logCallback);
                                }

                                if (상태.처리단계 != "전체약관")
                                {
                                    완료고객번호 = new ConcurrentBag<string>(tmp완료오류);
                                    if (await 실행(() => BatchUpdateAsync(상태, 완료고객번호, 오류고객번호, "모바일", logCallback), "모바일데이터업로드(BatchUpdateAsync)01", logCallback) == "실패")
                                    {
                                        throw new Exception("모바일데이터업로드(BatchUpdateAsync)01 오류, 로그참조");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                string 오류 = ex.Message;
                                모바일Transaction.Rollback();
                                약관Transaction?.Rollback();

                                if (재시도 == 0)
                                {
                                    foreach (var item in tmp완료오류)
                                        오류고객번호.Add(item);

                                    if (await 실행(() => BatchUpdateAsync(상태, 완료고객번호, 오류고객번호, "모바일", logCallback), "증권약관합치기(BatchUpdateAsync)02", logCallback) == "실패")
                                    {
                                        throw new Exception($"모바일데이터업로드(BatchUpdateAsync)02 오류, 로그참조 - {오류}");
                                    }
                                }
                                throw;
                            }
                        }
                    }
                    finally
                    {
                        모바일AdoNet?.Dispose();
                        약관AdoNet?.Dispose();
                    }
                    break;
                }
                catch (Exception)
                {
                    재시도++;
                    if (재시도 <= 3)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        continue;
                    }
                    실행여부 = "실패";
                    throw;
                }
            }
            return 실행여부;
        }

        // ==========================================================================================================
        // KB모바일 (장기) ==> 내용 Update
        // ==========================================================================================================
        public static async Task KBURL반환Table(Dictionary<string, string> updates, string 접속, Action<string, int> logCallback)
        {
            int batchSize = 300;
            var updateList = updates.ToList();
            logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [시작] KBURL반환Table 업데이트 (총 대상: {updateList.Count}건)", 1);

            using (var conn = new SqlConnection(접속))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        int totalUpdated = 0;

                        for (int i = 0; i < updateList.Count; i += batchSize)
                        {
                            var chunk = updateList.Skip(i).Take(batchSize).ToList();
                            var sqlBuilder = new System.Text.StringBuilder();

                            using (var cmd = new SqlCommand())
                            {
                                cmd.Connection = conn;
                                cmd.Transaction = transaction;

                                for (int j = 0; j < chunk.Count; j++)
                                {
                                    string paramUrl = $"@Url{j}";
                                    string paramPK = $"@PK{j}";

                                    sqlBuilder.AppendLine($"UPDATE MobileTermsMain SET PdfUrl = {paramUrl} WHERE PrimaryKey = {paramPK};");

                                    cmd.Parameters.AddWithValue(paramUrl, chunk[j].Value);
                                    cmd.Parameters.AddWithValue(paramPK, chunk[j].Key);
                                }

                                cmd.CommandText = sqlBuilder.ToString();
                                int 실제로적용된건수 = await cmd.ExecuteNonQueryAsync();
                                totalUpdated += 실제로적용된건수;
                            }
                        }
                        transaction.Commit();
                        string 고객번호목록 = string.Join(", ", updates.Keys);
                        logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [성공] KBURL반환Table 업데이트 완료. (처리완료: {totalUpdated}건)", 1);
                        logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [고객번호] {고객번호목록}", 1);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        logCallback?.Invoke($"{DateTime.Now:yy.MM.dd HH:mm:ss} [실패] KBURL반환Table 오류 발생으로 전체 롤백됨: {ex.Message}", 1);
                        throw;
                    }
                }
            }
        }

        // ==========================================================================================================
        // MSSQL fnGetKey ==> C#으로 변경
        // ==========================================================================================================
        public static string FnGetKeyCSharp(string cmpType, string insNo)
        {
            string input = cmpType + insNo;

            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }

                return sb.ToString();
            }
        }

        // ==========================================================================================================
        // 상태 업데이트 공통 모듈
        // ==========================================================================================================
        public static async Task<string> BatchUpdateAsync(
            현재상태 상태,
            ConcurrentBag<string> 완료,
            ConcurrentBag<string> 오류,
            string 현상태,
            Action<string, int> logCallback)
        {
            string 실행여부 = "성공";
            try
            {
                const int 묶음 = 100;
                상태.처리Step = "07";

                var 완료목록 = 완료.Distinct().ToList();
                string SqlStr = "";
                for (int i = 0; i < 완료목록.Count; i += 묶음)
                {
                    var part = 완료목록.Skip(i).Take(묶음).ToList();
                    string 키목록 = $"'{string.Join("','", part)}'";
                    SqlStr = $@"
                    UPDATE data 
                    SET {현상태}작업 = '{현상태}완료' 
                    WHERE groupKey = '{상태.groupKey}' AND
                          고객Key IN ({키목록})
                    ";

                    if (await 실행(() => DBUpdate(SqlStr, 상태.dataTable접속), "BatchUpdateAsync(DBUpdate)01", logCallback) == "실패")
                    {
                        throw new Exception("ER00_BatchUpdateAsync(DBUpdate)01 오류, 로그참조");
                    }
                }

                var 오류목록 = 오류.Distinct().ToList();
                for (int i = 0; i < 오류목록.Count; i += 묶음)
                {
                    var part = 오류목록.Skip(i).Take(묶음).ToList();
                    string 키목록 = $"'{string.Join("','", part)}'";
                    SqlStr = $@"
                    UPDATE data 
                    SET {현상태}작업 = '{현상태}오류' 
                    WHERE groupKey = '{상태.groupKey}' AND 
                          고객Key IN ({키목록})
                ";

                    if (await 실행(() => DBUpdate(SqlStr, 상태.dataTable접속), "BatchUpdateAsync(DBUpdate)02", logCallback) == "실패")
                    {
                        throw new Exception("ER00_BatchUpdateAsync(DBUpdate)02 오류, 로그참조");
                    }
                }
            }
            catch (Exception)
            {
                실행여부 = "실패";
                throw;
            }
            return 실행여부;
        }

        public static async Task<string> DBUpdate(string SqlStr, string dataTable접속, params SqlParameter[] parameters)
        {
            int 재시도 = 0;
            string 실행여부 = "";
            while (true)
            {
                try
                {
                    using (SqlConnection AdoNet = new SqlConnection(dataTable접속))
                    {
                        await AdoNet.OpenAsync();
                        using (SqlCommand sqlcmd = new SqlCommand(SqlStr, AdoNet))
                        {
                            sqlcmd.CommandTimeout = 0;
                            if (parameters != null && parameters.Length > 0)
                            {
                                sqlcmd.Parameters.Clear(); // 재시도 루프 시 중복 추가 방지
                                foreach (var p in parameters)
                                {
                                    // SqlParameter 객체는 하나의 Command에만 속할 수 있으므로, 
                                    // while 루프 안에서 안전하게 재사용하기 위해 값을 복사해서 넣습니다.
                                    sqlcmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value ?? DBNull.Value));
                                }
                            }

                            await sqlcmd.ExecuteNonQueryAsync();
                            실행여부 = "성공";
                            break; // 성공하면 루프 탈출
                        }
                    }
                }
                catch (Exception ex)
                {
                    재시도++;
                    if (재시도 <= 2)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        continue;
                    }
                    실행여부 = "실패";
                    throw new Exception($"DBUpdate 오류 : {ex.Message}");
                }
            }
            return 실행여부;
        }
    }
}