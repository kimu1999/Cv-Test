using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using OpenCvSharp;
using OpenCvSharp.Extensions;
using Tesseract;

using DrawingPoint = System.Drawing.Point;
using CvPoint = OpenCvSharp.Point;

namespace Cv_Test
{
    public partial class Form1 : Form
    {
        private Image masterImage;
        private float centerX;
        private float centerY;
        private Dictionary<string, string> filePathDictionary = new Dictionary<string, string>();
        private string processName = "None"; //초기화

        private bool isDrawing;
        private bool isRectangleMode;
        private DrawingPoint startPoint;
        private DrawingPoint endPoint;

        public Form1() //폼 초기화
        {
            InitializeComponent(); //구성요소 초기화
            trackBar_Threshold.Scroll += trackBar_Threshold_Scroll; //트랙바 초기화
            processout1.Text = ": None"; //초기화

            LeftPicture.MouseDown += leftDown;
            LeftPicture.MouseMove += leftMove;
            LeftPicture.MouseUp += leftUp;
            LeftPicture.Paint += leftPaint;

            int x = 0, y = 0, width = 100, height = 100; //초기화

            OpenCvSharp.Rect openCvRect = new OpenCvSharp.Rect(x, y, width, height);
            Tesseract.Rect tesseractRect = new Tesseract.Rect(x, y, width, height);
        }

        private void OpenImage_Click(object sender, EventArgs e) //이미지 오픈 
        {
            using (var openFileDialog = new OpenFileDialog()) //이미지 선택
            {
                openFileDialog.Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff";

                if (openFileDialog.ShowDialog() == DialogResult.OK) //bitmap으로 로드 
                {
                    var image = new Bitmap(openFileDialog.FileName);
                    LeftPicture.Image = image;
                    masterImage = image; //회전하기 위해 원본 이미지 저장
                    centerX = image.Width / 2.0f; //센터 좌표 
                    centerY = image.Height / 2.0f;

                    string fileName = System.IO.Path.GetFileName(openFileDialog.FileName);
                    string filePath = openFileDialog.FileName;

                    FileNameLabel.Text = ": " + fileName; //선택한 파일명.확장자 표시
                    FilePathLabel.Text = "File Path : " + filePath; //선택한 파일 경로 표시 
                }
            }
        }

        private void ColorToGray_Click(object sender, EventArgs e) //이미지 grayscale 적용
        {
            if (LeftPicture.Image == null) //왼쪽 창에 이미지 확인
            {
                MessageBox.Show("Please load an image first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (LeftPicture.Image is Bitmap bitmap) //이미지 있으면 오른쪽 창 gray적용
            {
                using (var src = BitmapConverter.ToMat(bitmap))
                using (var gray = new Mat())
                {
                    Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                    Cv2.Threshold(gray, gray, trackBar_Threshold.Value, 255, ThresholdTypes.Binary); //트랙바 값 받음
                    RightPicture.Image = BitmapConverter.ToBitmap(gray);
                }
                processName = ": Grayscale"; //프로세스 이름 지정
                processout1.Text = $"{processName}"; //프로세스 이름 출력
            }
            else
            {
                MessageBox.Show("Invalid Image format.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Rotation_Click(object sender, EventArgs e) //이미지 회전 
        {
            if (LeftPicture.Image == null)
            {
                MessageBox.Show("Please load an image first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (float.TryParse(AngleTextBox.Text, out float rotationAngle))
            {
                if (masterImage is Bitmap bitmap)
                {
                    Bitmap rotatedBitmap = new Bitmap(bitmap.Width, bitmap.Height);
                    rotatedBitmap.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);

                    using (Graphics g = Graphics.FromImage(rotatedBitmap))
                    {
                        g.TranslateTransform(centerX, centerY); //센터로 피벗을 옮김
                        g.RotateTransform(rotationAngle); //센터를 중심으로 돌려줌
                        g.TranslateTransform(-centerX, -centerY); //다시 피벗을 원위치 시킴
                        g.DrawImage(bitmap, PointF.Empty); //bitmap에 masterImage를 0,0 기준으로 그려줌
                    }
                    masterImage = rotatedBitmap;
                    LeftPicture.Image = rotatedBitmap; //새로운 이미지 대체
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid number for the rotation angle.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AngleTextBox_KeyDown(object sender, KeyEventArgs e) //각도 입력 후 엔터키 적용
        {
            if (e.KeyCode == Keys.Enter)
            {
                Rotation_Click(this, EventArgs.Empty);
                e.SuppressKeyPress = true;  //Enter 키 입력을 다른 컨트롤로 전달하지 않도록 방지
            }
        }

        private void Openfolder_Click(object sender, EventArgs e) //이미지 폴더 오픈
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    var files = System.IO.Directory.GetFiles(folderBrowserDialog.SelectedPath, "*.*")
                        .Where(file => file.ToLower().EndsWith("bmp") ||
                                       file.ToLower().EndsWith("jpg") ||
                                       file.ToLower().EndsWith("jpeg") ||
                                       file.ToLower().EndsWith("png") ||
                                       file.ToLower().EndsWith("tif") ||
                                       file.ToLower().EndsWith("tiff"))
                        .ToArray();
                    ImageListBox.Items.Clear();
                    foreach (var file in files) //파일명만 가져오기
                    {
                        string fileName = System.IO.Path.GetFileName(file);
                        ImageListBox.Items.Add(fileName);
                    }
                    filePathDictionary = files.ToDictionary(f => System.IO.Path.GetFileName(f), f => f);
                }
            }
        }

        private void ImageList_DoubleClick(object sender, EventArgs e) //폴더내 이미지들 리스트 업로드
        {
            if (ImageListBox.SelectedItem != null)
            {
                string fileName = ImageListBox.SelectedItem.ToString();//선택한 파일명 가져오기
                if (filePathDictionary.TryGetValue(fileName, out string filePath))//선택한 파일명에 해당하는 파일 경로 가져오기
                {
                    var image = new Bitmap(filePath);//파일 경로로부터 이미지 로드
                    LeftPicture.Image = image;
                    masterImage = image;
                    centerX = image.Width / 2.0f;//이미지의 중심 좌표 계산
                    centerY = image.Height / 2.0f;

                    FileNameLabel.Text = ": " + fileName;
                    FilePathLabel.Text = "File Path : " + filePath;
                }
            }
        }

        private void SaveImage_Click(object sender, EventArgs e) //rigthpicture 이미지 저장 
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff";
                saveFileDialog.DefaultExt = "png";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    RightPicture.Image.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                    MessageBox.Show("Image to Save", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void CannyEdge_Click(object sender, EventArgs e) //canny edge 검출
        {
            if (masterImage == null)
            {
                MessageBox.Show("Please load an image first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            using (var src = BitmapConverter.ToMat((Bitmap)masterImage)) // 이미지를 Mat형식으로 변환 
            using (var grayImage = new Mat())//그레이스케일 이미지 생성
            {
                Cv2.CvtColor(src, grayImage, ColorConversionCodes.BGR2GRAY);//이미지를 그레이스케일로 변환

                Mat edges = new Mat();//엣지 이미지 생성
                double threshold1 = trackBar_Threshold.Value;
                double threshold2 = trackBar_Threshold.Value * 2;
                Cv2.Canny(grayImage, edges, threshold1, threshold2);

                RightPicture.Image = BitmapConverter.ToBitmap(edges);
            }
            processName = ": Canny Edge";
            processout1.Text = $"{processName}";
        }

        private void Binary_Click(object sender, EventArgs e) //이진화 
        {
            if (masterImage == null)
            {
                MessageBox.Show("Please load an image first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            using (var src = BitmapConverter.ToMat((Bitmap)masterImage))
            using (var grayImage = new Mat())
            using (var binaryImage = new Mat())
            {
                Cv2.CvtColor(src, grayImage, ColorConversionCodes.BGR2GRAY); // 이미지를 그레이스케일로 변환
                Cv2.Threshold(grayImage, binaryImage, trackBar_Threshold.Value, 255, ThresholdTypes.Binary); // 이진화 적용
                RightPicture.Image = BitmapConverter.ToBitmap(binaryImage); // 이진화된 이미지를 PictureBox에 표시
            }
            processName = ": Binary";
            processout1.Text = $"{processName}";
        }

        private void Ocr_Click(object sender, EventArgs e) //tesseract 사용 ocr
        {
            if (LeftPicture.Image == null)
            {
                MessageBox.Show("Please load an image first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (LeftPicture.Image is Bitmap bitmap)//   이미지가 비트맵이면 OCR 적용
            {
                using (var ocrEngine = new TesseractEngine(@"C:\Users\HMK\Desktop\C# Test\Cv Test", "kor+kor_vert+eng", Tesseract.EngineMode.Default))//OCR 엔진 초기화
                {
                    using (var img = PixConverter.ToPix(bitmap))// 비트맵을 Pix 형식으로 변환 및 텍스트 추출
                    {
                        var result = ocrEngine.Process(img);
                        string extractedText = result.GetText(); //호출된 텍스트 가져옴
                        DisplayTextAsImage(extractedText); //메서드 호출하여 텍스트 이미지 변환

                        // 텍스트를 그림으로 그리기 위한 비트맵 생성
                        var textBitmap = new Bitmap(RightPicture.Width, RightPicture.Height);
                        using (Graphics g = Graphics.FromImage(textBitmap))
                        {
                            g.Clear(Color.White);
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                            // 텍스트 박스 크기 설정
                            RectangleF textRect = new RectangleF(10, 10, RightPicture.Width - 20, RightPicture.Height - 20);

                            // 텍스트를 그리기 위해 폰트와 브러시 설정
                            var font = new Font("Arial", 12);
                            var brush = Brushes.Black;

                            // 텍스트를 자동 줄바꿈하여 그리기
                            StringFormat format = new StringFormat();
                            format.FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.LineLimit;
                            g.DrawString(extractedText, font, brush, textRect, format);
                        }
                        RightPicture.Image = textBitmap; // RightPicture에 이미지를 표시
                    }
                    processName = ": OCR";
                    processout1.Text = $"{processName}";
                }
            }
            else
            {
                MessageBox.Show("Invalid Image format.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void trackBar_Threshold_Scroll(object sender, EventArgs e) //threshold 트랙바 컨트롤 
        {
            if (string.IsNullOrEmpty(processName))
            {
                processName = ": None";
            }
            if (processName.Contains("Grayscale"))
            {
                ColorToGray_Click(sender, e);
            }
            else if (processName.Contains("Canny Edge"))
            {
                CannyEdge_Click(sender, e);
            }
            else if (processName.Contains("Binary"))
            {
                Binary_Click(sender, e);
            }
        }

        private Rectangle GetRectangle(DrawingPoint p1, DrawingPoint p2) //선택한 영역의 직사각형 반환
        {
            return new Rectangle(
                Math.Min(p1.X, p2.X), //x좌표 중 작은 값
                Math.Min(p1.Y, p2.Y), // y좌표 중 작은 값
                Math.Abs(p1.X - p2.X), //x좌표 차이의 절대값
                Math.Abs(p1.Y - p2.Y)); //y좌표 차이의 절대값
        }

        private void Rectangle_Click(object sender, EventArgs e) //메서드 수정
        {
            isRectangleMode = true;
        }

        private void Circle_Click(object sender, EventArgs e)
        {
            isRectangleMode = false;
        }

        private void leftDown(object sender, MouseEventArgs e) //마우스 이벤트 추가
        {
            if (e.Button == MouseButtons.Left)
            {
                startPoint = new DrawingPoint(e.X, e.Y);
                isDrawing = true;
            }
        }

        private void leftMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                endPoint = new DrawingPoint(e.X, e.Y);
                LeftPicture.Invalidate(); //picturebox를 다시 그리게 요청
            }
        }

        private void leftUp(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                isDrawing = false;
                endPoint = e.Location;
                CropAndDisplay();
            }
        }
        
        private void leftPaint(object sender, PaintEventArgs e) //도형을 그리기 위한 leftpicture에 paint 추가
        {
            if(isDrawing)
            {
                var rect = GetRectangle(startPoint, endPoint);
                if (isRectangleMode)
                {
                    e.Graphics.DrawRectangle(Pens.Red, rect);
                }
                else
                {
                    e.Graphics.DrawEllipse(Pens.Red, rect);
                }
            }
        }

        private void CropAndDisplay() //특정  영역을 잘라 이미지 표시, 처리 작업을 적용
        {
            var rect = GetRectangle(startPoint, endPoint); //자를 영역의 직사각형 계산
            if (LeftPicture.Image is Bitmap bitmap)
            {
                using (var src = BitmapConverter.ToMat(bitmap)) //Bitmap이미지를 cv.mat형식으로 변환
                {
                    var boundedRect = rect;
                    boundedRect.Intersect(new Rectangle(0, 0, src.Width, src.Height)); //rect를 크기 내에 제한하기 위한 메서드
                    if(boundedRect.Width > 0 && boundedRect.Height > 0)
                    {
                        var roi = new Mat(src, new OpenCvSharp.Rect(boundedRect.X, boundedRect.Y, boundedRect.Width, boundedRect.Height)); 
                        var croppedBitmap = BitmapConverter.ToBitmap(roi); //roi를 활용하여 잘라낸 영역을 다시 Bitmap으로 변환

                        LeftPicture.Image = croppedBitmap;
                        ApplyCurrentProcess(croppedBitmap); //메서드 호출하여 현재 이미지 처리 작업 적용

                        masterImage = croppedBitmap; //croppedBitmap을 masterImage로 설정
                    }
                }
            }
        }

        private void ApplyCurrentProcess(Bitmap croppedBitmap) // 프로세스 이름에 따라 해당 프로세스 실행
        {
            if (processName.Contains("Grayscale"))
            {
                ApplyGrayscale(croppedBitmap);
            }
            else if (processName.Contains("Canny Edge"))
            {
                ApplyCannyEdge(croppedBitmap);
            }
            else if (processName.Contains("Binary"))
            {
                ApplyBinary(croppedBitmap);
            }
            else if (processName.Contains("OCR"))
            {
                ApplyOCR(croppedBitmap);
            }
        }

        private void ApplyGrayscale(Bitmap croppedBitmap)
        {
            using (var src = BitmapConverter.ToMat(croppedBitmap))
            using (var gray = new Mat())
            {
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                RightPicture.Image = BitmapConverter.ToBitmap(gray);
            }
        }

        private void ApplyCannyEdge(Bitmap croppedBitmap)
        {
            using (var src = BitmapConverter.ToMat(croppedBitmap))
            using (var grayImage = new Mat())
            {
                Cv2.CvtColor(src, grayImage, ColorConversionCodes.BGR2GRAY);
                Mat edges = new Mat();
                Cv2.Canny(grayImage, edges, trackBar_Threshold.Value, trackBar_Threshold.Value * 2);
                RightPicture.Image = BitmapConverter.ToBitmap(edges);
            }
        }

        private void ApplyBinary(Bitmap croppedBitmap)
        {
            using (var src = BitmapConverter.ToMat(croppedBitmap))
            using (var grayImage = new Mat())
            using (var binaryImage = new Mat())
            {
                Cv2.CvtColor(src, grayImage, ColorConversionCodes.BGR2GRAY);
                Cv2.Threshold(grayImage, binaryImage, trackBar_Threshold.Value, 255, ThresholdTypes.Binary);
                RightPicture.Image = BitmapConverter.ToBitmap(binaryImage);
            }
        }

        private void ApplyOCR(Bitmap croppedBitmap)
        {
            using (var ocrEngine = new TesseractEngine(@"C:\Users\HMK\Desktop\C# Test\Cv Test", "kor+kor_vert+eng", Tesseract.EngineMode.Default))
            {
                using (var img = PixConverter.ToPix(croppedBitmap))
                {
                    var result = ocrEngine.Process(img);
                    string extractedText = result.GetText();
                    DisplayTextAsImage(extractedText);
                }
            }
        }

        private void DisplayTextAsImage(string extractedText) //문자열을 이미지로 변환, 사용자 인터페이스의 특정 컨트롤에 표시
        {
            var textBitmap = new Bitmap(RightPicture.Width, RightPicture.Height); 
            using (Graphics g = Graphics.FromImage(textBitmap)) //grapchis 객체를 생성해 textbitmap에 그릴 수 있게 함
            {
                g.Clear(Color.White); //배경색을 흰색으로 설정
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;//그림을 부드럽게 그리기 위해 안티앨리어싱 사용
                RectangleF textRect = new RectangleF(10, 10, RightPicture.Width - 20, RightPicture.Height - 20);//  텍스트 박스 크기 설정
                var font = new Font("Arial", 12);//폰트 설정
                var brush = Brushes.Black;//    브러시 설정
                StringFormat format = new StringFormat();// 텍스트를 그리기 위한 포맷 설정, +밑 줄 코드를 사용해 텍스트가 잘리지 않고 줄 바꿈을 제한
                format.FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.LineLimit;
                g.DrawString(extractedText, font, brush, textRect, format);//지정된 서식등을 가지고 문자열을 textbitmap에 그림
            }
            RightPicture.Image = textBitmap;
        }
    }
}

    