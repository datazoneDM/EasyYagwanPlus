# Easy약관Plus (EasyYakgwanPlus)

고객별 맞춤형 보험약관 자동 생성, 세션별 PDF 병합, 보험증권 결합 및 북마크(Outline) 자동 생성을 수행하는 C# WinForms 기반 오픈소스 프로젝트입니다.

## 1. 시스템 구성 및 아키텍처

본 저장소는 맞춤약관 생성 및 PDF 처리를 담당하는 핵심 애플리케이션으로 구성되어 있습니다.

### 1) Easy약관Plus
*   **맞춤약관 조합 (`frm담보Merge.cs`)**: 고객별 계약 데이터를 기반으로 SQL Server에서 보통약관, 특별약관, 제도성약관, 별표 및 동적 목차를 추출하여 가변 약관을 생성합니다.
*   **증권 및 약관 PDF 병합**: **iText 7** 라이브러리를 활용하여 보험증권 PDF와 맞춤약관 PDF를 병합하고, 발송 구분에 따라 양면 인쇄용 보정 및 주소 표지를 생성합니다.
*   **PDF 북마크 재구성**: 결합된 약관에 쪽번호를 스탬핑하고, 동적으로 삽입된 목차 페이지 위치에 맞춰 PDF 북마크(Outline)를 재구성합니다.

### 2) Easy약관PDF - H2PControl
*   **실시간 분산 제어 관제탑 (`H2PControl.cs`)**: TCP/IP 소켓 통신을 통해 메인(Server) PC와 서브(Client) PC 간 작업 물량을 동적으로 분배하고 장애 시 자동 복구 및 재할당을 수행합니다.
*   **세션별 PDF 병합**: 변환 워커가 생성한 세션 PDF들을 **iText 7**을 사용하여 단일 약관 PDF로 결합하고 지정된 위치에 목차(IX)를 삽입합니다.

## 2. 개발 환경
*   **OS**: Windows 10 / 11
*   **Framework**: .NET Framework (C# WinForms)
*   **Database**: Microsoft SQL Server
*   **의존성**: iText 7, DevExpress (빌드 시 필요), 한컴오피스 한글 (HWP ActiveX)

## 3. 라이선스 (License)
본 프로젝트는 **iText 7 Core** 라이브러리를 사용하며, **GNU Affero General Public License v3.0 (AGPL-3.0)** 하에 소스 코드를 공개합니다. 상세 내용은 `LICENSE` 및 `NOTICE.txt`를 참조하십시오.
