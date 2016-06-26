using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Media.Imaging;

namespace MetroTips091CS
{
    public sealed partial class MyUserControl : UserControl
    {
        #region // 変数 //
        /// <summary>
        /// OCRに掛ける画像
        /// </summary>
        private Windows.UI.Xaml.Media.Imaging.WriteableBitmap _bitmap;

        /// <summary>
        /// フォルダから取得した画像ファイルリスト
        /// </summary>
        private List<StorageFile> ImageFileList = new List<StorageFile>();

        /// <summary>
        /// 画像リストのカレントインデックス
        /// </summary>
        private int _CurrentIdx = 0;
        #endregion

        #region // プロパティ //
        /// <summary>
        /// 画像リストのカレントインデックス
        /// </summary>
        private int CurrentIdx
        {
            get
            {
                return _CurrentIdx;
            }
            set
            {
                if (ImageFileList.Count > 0 && value >= 0 && value < ImageFileList.Count)
                {
                    _CurrentIdx = value;

                    ImageCount.Text = string.Format("{0}/{1}", CurrentIdx + 1, ImageFileList.Count);

                    // 画像ファイルロード
                    LoadFileEx(ImageFileList[_CurrentIdx]);
                }
                else
                {
                    ImageCount.Text = "0/0";
                }

                // ボタンマスク制御
                ButtonEnabledCtrl();
            }
        }
        #endregion

        #region // コンストラクタ //
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MyUserControl()
        {
            this.InitializeComponent();

            // ボタンマスク制御
            ButtonEnabledCtrl();
        }
        #endregion
        
        #region // 内部関数 //
        /// <summary>
        /// 画像ファイル読込
        /// </summary>
        /// <param name="file"></param>
        private async void LoadFileEx(StorageFile file)
        {
            await LoadSampleImage(file);
        }

        /// <summary>
        /// 画像ファイル読込
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private async System.Threading.Tasks.Task LoadSampleImage(StorageFile file)
        {
            _bitmap = await LoadImageAsync(file);
            this.SampleImage.Source = _bitmap;

            // 日本語OCR
            if (_bitmap != null)
            {
                await ExtractTextAsync(_bitmap, WindowsPreview.Media.Ocr.OcrLanguage.Japanese);
            }
        }

        /// <summary>
        /// Bitmap取得
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private async static System.Threading.Tasks.Task<Windows.UI.Xaml.Media.Imaging.WriteableBitmap>
          LoadImageAsync(Windows.Storage.StorageFile file)
        {
            Windows.Storage.FileProperties.ImageProperties imgProp = await file.Properties.GetImagePropertiesAsync();
            using (var imgStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                var bitmap = new Windows.UI.Xaml.Media.Imaging.WriteableBitmap((int)imgProp.Width, (int)imgProp.Height);
                bitmap.SetSource(imgStream);
                return bitmap;
            }
        }

        /// <summary>
        /// 指定フォルダからファイル取得
        /// </summary>
        /// <param name="filesInFolder"></param>
        private void GetFiles(IReadOnlyList<StorageFile> filesInFolder)
        {
            ImageFileList = new List<StorageFile>();
            foreach (StorageFile file in filesInFolder)
            {
                string Ext = file.FileType.ToLower();
                if (Ext == ".png" || Ext == ".jpg" || Ext == ".jpeg")
                {
                    ImageFileList.Add(file);
                }
            }

            if (ImageFileList.Count > 0)
            {
                CurrentIdx = 0;
            }
        }

        /// <summary>
        /// OCR認識
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        private async Task ExtractTextAsync(WriteableBitmap bitmap, WindowsPreview.Media.Ocr.OcrLanguage language)
        {
            // OcrEngineオブジェクトを生成する
            var ocrEngine = new WindowsPreview.Media.Ocr.OcrEngine(language);

            // OcrEngineに画像を渡して、文字列を認識させる
            var ocrResult = await ocrEngine.RecognizeAsync(
                                    (uint)bitmap.PixelHeight,     // 画像の高さ
                                    (uint)bitmap.PixelWidth,      // 画像の幅
                                    bitmap.PixelBuffer.ToArray()  // 画像のデータ（バイト配列）
                                  );
            if (ocrResult.Lines == null || ocrResult.Lines.Count == 0)
            {
                this.ReadText.Text = "(何も読み取れませんでした)";
                return;
            }

            // Word間の区切り（日本語では無し、英語ではスペースとする）
            var separater = string.Empty;
            if (language == WindowsPreview.Media.Ocr.OcrLanguage.English)
                separater = " ";

            // 結果を表示するテキストボックスをクリアする
            this.ReadText.Text = string.Empty;

            // 行番号（0始まり）を定義
            int lineIndex = 0;

            // 認識結果を行ごとに処理する
            foreach (var line in ocrResult.Lines)
            {
                // 1行分の文字列を格納するためのバッファ
                var sb = new System.Text.StringBuilder();

                // 認識結果の行を、Wordごとに処理する
                foreach (var word in line.Words)
                {
                    // 認識した文字列をバッファに追加していく
                    sb.Append(word.Text);
                    sb.Append(separater);

                    // word は OcrWord 型。Textプロパティ(string型)の他に、
                    // その文字列を読み取った場所を表すLeft／Top／Widht／Heightプロパティ(int型)も持っている。
                    // なお、OcrWord という名前だが、日本語では1文字になる。
                }

                //// ここでは、読み取った1行を以下のフォーマットで表示することにした
                //this.ReadText.Text += string.Format("[{0}{1}] {2}{3}",
                //                                    (lineIndex++).ToString(),     // 行番号(0始まり)
                //                                    line.IsVertical ? "V" : "H",  // 縦書き／横書きの区別
                //                                    sb.ToString().TrimEnd(),      // 読み取った文字列
                //                                    Environment.NewLine           // 改行
                //                                   );
                
                this.ReadText.Text += string.Format("{0}{1}",
                    sb.ToString().TrimEnd(),      // 読み取った文字列
                    Environment.NewLine           // 改行
                    );

            }

            // 他に ocrResult.TextAngle (読み取った文字列群の、水平(または垂直)からの回転角度) も取得できる。
            // OcrWord 型の示す場所は、この TextAngle の分だけ回転している。
        }

        /// <summary>
        /// ボタンマスク制御
        /// </summary>
        private void ButtonEnabledCtrl()
        {
            bool bFirst = true;
            bool bPrev = true;
            if (CurrentIdx == 0)
            {
                bFirst = false;
                bPrev = false;
            }

            bool bNext = true;
            bool bLast = true;

            if (CurrentIdx >= ImageFileList.Count - 1)
            {
                bNext = false;
                bLast = false;
            }

            Button_First.IsEnabled = bFirst;
            Button_Prev.IsEnabled = bPrev;
            Button_Next.IsEnabled = bNext;
            Button_Last.IsEnabled = bLast;
        }
        #endregion

        #region // イベント処理 //
        /// <summary>
        /// フォルダ参照クリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");

            StorageFolder pickedFolder = await picker.PickSingleFolderAsync();
            if (pickedFolder != null)
            {
                ImageFileList = new List<StorageFile>();
                IReadOnlyList<StorageFile> filesInFolder = await pickedFolder.GetFilesAsync();

                // 画像ファイル取得
                GetFiles(filesInFolder);

                // フォルダパス表示
                TextBox_Path.Text = pickedFolder.Path;
            }
        }

        /// <summary>
        /// 日本語ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void JapaneseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_bitmap != null)
            {
                await ExtractTextAsync(_bitmap, WindowsPreview.Media.Ocr.OcrLanguage.Japanese);
            }
        }

        /// <summary>
        /// 英語ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void EnglishButton_Click(object sender, RoutedEventArgs e)
        {
            if (_bitmap != null)
            {
                await ExtractTextAsync(_bitmap, WindowsPreview.Media.Ocr.OcrLanguage.English);
            }
        }

        /// <summary>
        /// 最初
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            CurrentIdx = 0;
        }

        /// <summary>
        /// 前へ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            CurrentIdx--;
        }

        /// <summary>
        /// 次へ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            CurrentIdx++;
        }

        /// <summary>
        /// 最後
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            CurrentIdx = ImageFileList.Count - 1;
        }

        /// <summary>
        /// サイズ変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            maingrid.Height = stackpanel.ActualHeight - headgrid.Height;

            ReadText.Height = stackpanel.ActualHeight - headgrid.Height - ocrbtngrid.Height;
        }
        #endregion
    }
}
