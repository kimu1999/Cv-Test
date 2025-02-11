C# winform을 사용한 이미징 처리 프로그램 
프로젝트 개요 
1. 배경
  - 머신비전 국비 수업을 배우는 중 C#을 활용해서 어떤 프로그램을 만들어보면 좋을까로 시작함
  - OpenCV 라이브러리를 활용하면 좋을거 같아서 최대한 활용

2. H/W
   - 노트북
  
3. S/W
   - OS : Window 11
   - IDE : Visual Studio Studio
   - Language : C#
   - Library : OpenCV
   - PaltForm : Winform
  
4. 주요 기능 
  0. Main page
   ![Image](https://github.com/user-attachments/assets/22ecb04d-d467-49ec-807f-9f965f8e8aa1)
  1. File
     - Open Image
     - Open Folder
  2. Save Image
  3. Image Process
     - Color To Gray
       ![Image](https://github.com/user-attachments/assets/ce684c5e-71a4-4e3e-b767-41efc7f6a8fe)
      
     - Rotation
     - Canny Edge
       ![Image](https://github.com/user-attachments/assets/7ec50561-6294-4473-ad18-8a7534ddc77e)
      
     - Binary
       ![Image](https://github.com/user-attachments/assets/94e27824-f860-4119-8063-8d51b2cd6685)
      
     - OCR
       ![Image](https://github.com/user-attachments/assets/57f9840b-a1e2-4e2e-a352-dba9730cc04a)
  
  4. ROI
     - Rectangle
     - Circle
  5. Others
     - File Path
     - Threshold
5. 후기
    5.1 아쉬운 점
       - ROI를 완벽하게 적용을 못시키는 점(영역을 표시하면 다른 부분이 확대가 되는 에러발생)
       - 폴더 내 폴더 열기 기능을 구현 못한 점
       - Github를 이용하여 형상관리를 처음해본 것과 C# winform 코드를 어떻게 올려야 하는지 몰라 sin파일로만 지속해서 올렸던 점 
  
