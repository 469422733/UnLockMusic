using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;
using System.IO;
using System.Threading;

/**
 *  V1.2     检查到bug：issue-0        输入框为空时，可以搜索歌曲，点击试听发生错误  
 *  V1.2.1   修改                      增加搜索歌曲线程、试听歌曲线程、滚动条等待
 *           未解决issue-0
 **/
namespace UnLockMusic
{
    public partial class frmList : Form
    {
        private int m_intFormat = 0;                    //保存格式：0-歌名，1-歌名（副标题），2-歌名（副标题）[歌手]，3-歌名[歌手]
        private string m_strDocument = "Music";         //保存音乐的文件夹
        private string m_strTempData = "TempData";      //临时文件夹
        private string m_strLog = "log.txt";            //日志文件
        private string m_strConfig = "config.txt";      //配置文件
        private bool isSelectAllMusic = false;          //将全选按钮和取消按钮合并

        private const int numbering = 1;
        private const int name = 2;
        private const int singer = 3;
        private const int album = 4;

        //定义回调
        private delegate void SetTipCallBack(string strText);
        private delegate void SetMusicInfoCallBack(string FilePath, string Title, string Author, string Description = "");
        private delegate void SetDataGVscanCallBack(int rowNum, clsMusic music);
        
        //声明回调
        private SetTipCallBack TipCallBack;
        private SetMusicInfoCallBack MusicInfoCallBack;
        private SetDataGVscanCallBack DataGVscanCallBack;

        public frmList()
        {
            InitializeComponent();
            init();
            ReadConfig();
        }

        #region 控件事件
        private void dataGVscan_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            lblTip.Text = "";
            if (!RowCanDownload(dataGVscan.CurrentRow.Index))
            {
                lblTip.Text = "该音乐无法下载。";
                return;
            }

            if ((dataGVscan.CurrentCell.ColumnIndex == 2) || (dataGVscan.CurrentCell.OwningColumn.Name == "dgvPlayMusic"))
            {
                WaitBar.ResetText();
                WaitBar.Show();
                WaitBar.MarqueeAnimationSpeed = 30;
                new Thread(new ParameterizedThreadStart(PlayMusic)).Start(dataGVscan.CurrentRow.Index);
                return;
            }
            
            //Download
            if (dataGVscan.CurrentCell.OwningColumn.Name == "dgvDownload")
            {
                DownloadMusic(Convert.ToInt32(dataGVscan.CurrentRow.Index));
                lblTopTip.Text = "this is add download";
                return;
            }

            if (this.dataGVscan.CurrentRow.Cells[0].EditedFormattedValue.ToString() == "True")
            {
                this.dataGVscan.CurrentRow.Cells[0].Value = false;
            }
            else
            {
                //for (int i = 0; i < this.dataGVscan.RowCount; i++)
                //{
                //    this.dataGVscan.Rows[i].Cells[0].Value = false;
                //}
                this.dataGVscan.CurrentRow.Cells[0].Value = true;

            }
        }
        private void dataGVscan_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                selectAll();
            }
            if (e.ColumnIndex == 7)
            {
                downloadSelected();
            }
        }
        private void txbSerch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 13)
                btnSerch_Click(sender, e);
        }

        /*
         *搜索歌曲
         */
        private void btnSerch_Click(object sender, EventArgs e)
        {
            WaitBar.ResetText();
            WaitBar.Show();
            WaitBar.MarqueeAnimationSpeed = 30;
            ClearForm();
            SearchMusic();
          
        }
        private void  selectAll()
        {
            isSelectAllMusic = !isSelectAllMusic;
            if (isSelectAllMusic)
            {
                for (int i = 0; i < this.dataGVscan.RowCount; i++)
                {
                    if (RowCanDownload(i))
                        this.dataGVscan.Rows[i].Cells[0].Value = true;
                }
            }
            else
            {
                for (int i = 0; i < this.dataGVscan.RowCount; i++)
                {
                    this.dataGVscan.Rows[i].Cells[0].Value = false;
                }
            }
        }
        private void btnCancelSelect_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < this.dataGVscan.RowCount; i++)
            {
                this.dataGVscan.Rows[i].Cells[0].Value = false;
            }
        }
        private void downloadSelected()
        {
            for (int i = 0; i < dataGVscan.RowCount; i++)
            {
                if (dataGVscan.Rows[i].Cells[0].EditedFormattedValue.ToString() == "True")
                {
                    DownloadMusic(i);
                }
            }
        }
        private void cmbFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_intFormat = cmbFormat.SelectedIndex;
        }
      
        private void loopPlay_CheckedChanged(object sender, EventArgs e)
        {
            CheckState isLoopPlay = loopPlay.CheckState;
            if (isLoopPlay == CheckState.Checked)
            {
                axWindowsMediaPlayer1.settings.setMode("loop", true);
            }
            else
            {
                axWindowsMediaPlayer1.settings.setMode("loop", false);
            }
        }

        private void watchHelp_Click(object sender, EventArgs e)
        {
            HelpForm helpForm = new HelpForm();
            helpForm.Show();
        }

        private void frmList_FormClosing(object sender, FormClosingEventArgs e)
        {
            lblTip.Text = "正在努力关闭……………………";
            axWindowsMediaPlayer1.Ctlcontrols.pause();
            axWindowsMediaPlayer1.URL = "";
            string strFileName = m_strDocument + "\\" + m_strTempData;
            if (Directory.Exists(strFileName))   //如果存在临时文件夹，就删除  
                Directory.Delete(strFileName, true);
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (axWindowsMediaPlayer1.currentMedia != null)
                lblMusicTime.Text = axWindowsMediaPlayer1.Ctlcontrols.currentPositionString + "/" + axWindowsMediaPlayer1.currentMedia.durationString;
            else
            {
                lblMusicTime.Text = "00:00/00:00";
                timer1.Enabled = false;
            }
        }
        private void btnOpenDirectory_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(m_strDocument))//存在，直接打开
                System.Diagnostics.Process.Start("explorer.exe", m_strDocument);
            else                                //不存在，创建，打开
            {
                Directory.CreateDirectory(m_strDocument);
                System.Diagnostics.Process.Start("explorer.exe", m_strDocument);
            }
        }
        #endregion

        #region 界面函数
        private void init()
        {
            //设置 播放器 的初始设置和位置
            axWindowsMediaPlayer1.settings.autoStart = true;
            axWindowsMediaPlayer1.settings.volume = 100;
            axWindowsMediaPlayer1.Width = dataGVscan.Width;
            axWindowsMediaPlayer1.Height = 45;
            axWindowsMediaPlayer1.Top = dataGVscan.Top + dataGVscan.Height + 30;
            axWindowsMediaPlayer1.Left = dataGVscan.Left;

            cmbFormat.Items.Clear();
            //cmbFormat.Items.Add("歌名");
            cmbFormat.Items.AddRange(new object[] {
            "歌手 - 歌名",
            "歌名（副标题）",
            "歌名（副标题）[歌手]",
            "歌名[歌手]",
            "歌名（歌手）",
            "歌名",
            "歌名 - 歌手"});
            cmbFormat.SelectedIndex = 0;
        }
        /// <summary>
        /// 清除原先界面信息
        /// </summary>
        private void ClearForm()
        {
            lblTopTip.Text = "点击歌曲名称可以试听。";
            lblTip.Text = "";
        }
        #endregion

        #region 操作功能函数
        /// <summary>
        /// 获取列表
        /// </summary>
        
        private void SearchMusic()
        {
            TipCallBack = new SetTipCallBack(SetTipText);//实例化回调
            MusicInfoCallBack = new SetMusicInfoCallBack(SetMusicInfo);
            DataGVscanCallBack = new SetDataGVscanCallBack(AddAMusic);
            Thread searchMusicThread = new Thread(new ThreadStart(GetMusicList));
            searchMusicThread.Start();
        }
        private void GetMusicList()
        {
            string strName = txbSerch.Text;
            clsMusicOperation mop = new clsMusicOperation();
            List<clsMusic> lmsc = new List<clsMusic>();

            try
            {
                dataGVscan.Invoke((MethodInvoker)(() =>  //清空列表
                {
                        dataGVscan.Rows.Clear();
                    }));  
                lmsc = mop.GetMusicList(strName);
                for (int i = 0; i < lmsc.Count(); i++)
                {
                    WaitBar.Invoke((MethodInvoker)(() =>
                    {
                        WaitBar.Hide();
                    }));
                    dataGVscan.Invoke(DataGVscanCallBack, i, lmsc[i]);
                }
                WaitBar.Invoke((MethodInvoker)(() =>
                    {
                        WaitBar.Hide();
                    }));
                // todo: 发生错误，返回参数，显示音乐列表
                lblTip.Invoke(TipCallBack, "搜索完毕。（tip：网络不好的话，试听会卡住，特别是酷我音乐多是高品质。）");
            }
            catch (Exception e)
            {
                // todo: 发生错误，返回参数，不显示音乐列表
                lblTip.Invoke(TipCallBack, "发生错误，错误信息：" + e.Message);
                WriteLog("搜索发生错误，错误信息：" + e.Message);
            }
        }
        /// <summary>
        /// 下载音乐
        /// </summary>
        /// <param name="iRow">列表中的行</param>
        private void DownloadMusic(int iRow)
        {
            TipCallBack = new SetTipCallBack(SetTipText);//实例化回调
            MusicInfoCallBack = new SetMusicInfoCallBack(SetMusicInfo);
            //lblTip.Text = "开启下载进程……";
            object oRow = (object)iRow;
            Thread thd = new Thread(new ParameterizedThreadStart(DownloadMusicThread));
            thd.Start(oRow);

        }
        /// <summary>
        /// 下载音乐的线程
        /// </summary>
        /// <param name="oRow">object oRow = (object)iRow;</param>
        private void DownloadMusicThread(object oRow)
        {
            int iRow = (int)oRow;
            string strDownloadURL = "";
            string strName = dataGVscan.Rows[iRow].Cells["dgvName"].Value.ToString();
            string strSinger = dataGVscan.Rows[iRow].Cells["dgvSinger"].Value.ToString();
            string strDownloadInfo = dataGVscan.Rows[iRow].Cells["dgvDownloadInfo"].Value.ToString();
            //int intSource = Convert.ToInt32(dataGVscan.Rows[iRow].Cells["dgvSource"].Value);
            enmMusicSource emsSource = (enmMusicSource)dataGVscan.Rows[iRow].Cells["dgvSource"].Value;
            string strFileName = m_strDocument;
            clsMusicOperation mop = new clsMusicOperation();
            clsHttpDownloadFile hdf = new clsHttpDownloadFile();
            string strTempFile = m_strDocument + "\\" + m_strTempData + "\\" + strDownloadInfo;//临时文件
            string strFormat = ".mp3";
            int i = 0;
            bool bolExistsTempFile = false;//是否存在缓存
            bool bolDownloadStatus = false;//是否下载成功

            //lblTip.Text = "正在下载……";
            //SetTipText("正在下载……" + strName);
            lblTip.Invoke(TipCallBack, "正在下载…… " + strName);//调用回调//因为线程无法条用主线的一些控件，必须通过回调函数实现
            try
            {
                if (!Directory.Exists(strFileName))   //如果不存在 m_strDocument 就创建  
                    Directory.CreateDirectory(strFileName);

                if ((File.Exists(strTempFile + ".mp3")) || (File.Exists(strTempFile + ".m4a")))//如果存在缓存，不再下载，直接复制
                {
                    bolExistsTempFile = true;
                    if ((File.Exists(strTempFile + ".mp3")))
                    {
                        strTempFile = strTempFile + ".mp3";
                        strFormat = ".mp3";
                    }
                    if (File.Exists(strTempFile + ".m4a"))
                    {
                        strTempFile = strTempFile + ".m4a";
                        strFormat = ".m4a";
                    }
                }
                else    //如果不存在，则需要先获取URL，再下载
                {
                    strDownloadURL = mop.GetMusicDownloadURL(strDownloadInfo, emsSource);
                    if (strDownloadURL == "")
                    {
                        lblTip.Invoke(TipCallBack, "无法获取歌曲“" + strName + "”的下载地址，下载失败！");
                        WriteLog("无法获取歌曲“" + strName + "”的下载地址，下载失败！");
                        return;
                    }
                    strFormat = mop.GetFileFormat();
                }

                strFileName = strFileName + "\\" + GetFileName(iRow) + strFormat;//设置文件名
                while (File.Exists(strFileName))  //如果存在文件，则添加序号，直到获取到一个可创建的文件名
                {
                    i++;
                    if (i == 1)
                        strFileName = strFileName.Insert(strFileName.Length - 4, "(" + i + ")");//
                    else
                        strFileName = strFileName.Replace("(" + (i - 1) + ")", "(" + i + ")");
                }

                if (bolExistsTempFile)
                {
                    File.Copy(strTempFile, strFileName, true);//三个参数分别是源文件路径，存储路径，若存储路径有相同文件是否替换
                    //File.Move(strTempFile.Replace("\\" + m_strTempData, ""), strFileName);//修改文件名
                    bolDownloadStatus = true;
                }
                else
                {
                    if (hdf.Download(strDownloadURL, strFileName))
                    {
                        //SetTipText(strName + "下载完成！");
                        //lblTip.Text = "下载完成！";
                        //mop.ChangeFileAttribute(strFileName, dataGVscan.Rows[iRow].Cells["dgvName"].Value.ToString(), dataGVscan.Rows[iRow].Cells["dgvSinger"].Value.ToString());
                        bolDownloadStatus = true;
                    }
                    else
                    {
                        //SetTipText("存在歌曲下载失败，请查看日志！");
                        //lblTip.Text = "下载失败，该名称歌曲可能已经存在，请修改Music文件下的同名歌曲；或者网络出现问题，请检查网络！";
                        WriteLog("歌曲：" + strName + " 下载失败，可能网络出现问题，请检查网络！");
                        //MessageBox.Show("歌曲：" + strName + " 下载失败，该名称歌曲可能已经存在，请修改Music文件下的同名歌曲；或者网络出现问题，请检查网络！");
                        lblTip.Invoke(TipCallBack, "歌曲：" + strName + " 下载失败，可能网络出现问题，请检查网络！");
                        bolDownloadStatus = false;
                    }
                }
                if (bolDownloadStatus)
                {
                    WriteLog(strName + " 下载完成！");
                    lblTip.Invoke(TipCallBack, strName + " 下载完成！");
                    if (strFormat.ToLower() == ".mp3")
                        //axWindowsMediaPlayer2.Invoke(MusicInfoCallBack, strFileName, strName, strSinger, "");
                        SetMusicInfo(strFileName, strName, strSinger);//设置歌曲信息
                }
            }
            catch (Exception e)
            {
                //SetTipText("下载线程发生错误，请查看日志！错误信息：" + e.Message);
                //lblTip.Text = "发生错误，错误信息：" + e.Message;
                WriteLog("歌曲：" + strName + " 下载失败，错误信息：" + e.Message);
                //MessageBox.Show("歌曲：" + strName + " 下载失败，错误信息：" + e.Message);
                lblTip.Invoke(TipCallBack, "歌曲：" + strName + " 下载失败，错误信息：" + e.Message);
            }
        }
        private void PlayMusic(object oRow)
        {
            //lblTopTip.Text = "";
            int iRow = (int)oRow;
            string strDownloadURL = "";
            enmMusicSource emsSource = (enmMusicSource)dataGVscan.Rows[iRow].Cells["dgvSource"].Value;
            string strDownloadInfo = dataGVscan.Rows[iRow].Cells["dgvDownloadInfo"].Value.ToString();
            string strDisplayName = dataGVscan.Rows[iRow].Cells["dgvDisplayName"].Value.ToString();
            string strID = dataGVscan.Rows[iRow].Cells["dgvID"].Value.ToString();
            string strFileName = m_strDocument + "\\" + m_strTempData;
            clsMusicOperation mop = new clsMusicOperation();
            clsHttpDownloadFile hdf = new clsHttpDownloadFile();
            try
            {
                if (!Directory.Exists(strFileName))   //如果不存在就创建 临时文件夹  
                    Directory.CreateDirectory(strFileName);

                strDownloadURL = mop.GetMusicDownloadURL(strDownloadInfo, emsSource);
                if (strDownloadURL == "")
                {
                    WaitBar.Invoke((MethodInvoker)(() =>
                    {
                        WaitBar.Hide();
                    }));
                    lblTip.Invoke((MethodInvoker)(()=>
                    {
                        lblTip.Text = "无法获取歌曲“" + strDisplayName + "”的下载地址，试听失败！";
                    }));
                    WriteLog("无法获取歌曲“" + strDisplayName + "”的下载地址，试听失败！");
                    return;
                }
                strFileName = strFileName + "\\" + strDownloadInfo + mop.GetFileFormat(); //临时文件夹 + dgvDownloadInfo + 格式

                if (!(File.Exists(strFileName))) //不存在缓存，才下载
                    if (!hdf.Download(strDownloadURL, strFileName))
                    {
                        WaitBar.Invoke((MethodInvoker)(() =>
                        {
                            WaitBar.Hide();
                        }));
                        lblTip.Invoke((MethodInvoker)(() =>
                        {
                            lblTip.Text = "播放发生错误，错误信息：获取音乐缓存失败。";
                        }));
                        return;
                    }
                WaitBar.Invoke((MethodInvoker)(() =>
                {
                    WaitBar.Hide();
                }));
                //播放音乐
                lblTip.Invoke((MethodInvoker)(() =>
                {
                    lblTip.Text = "播放音乐序号：" + strID + "  歌曲名：" + strDisplayName;
                }));
                axWindowsMediaPlayer1.URL = strFileName;
                axWindowsMediaPlayer1.Ctlcontrols.play();
                //lblMusicTime.Text = axWindowsMediaPlayer1.currentMedia.durationString;
                timer1.Enabled = true;
            }
            catch (Exception e)
            {
                WaitBar.Invoke((MethodInvoker)(() =>
                {
                    WaitBar.Hide();
                }));
                lblTip.Invoke((MethodInvoker)(() =>
                {
                    lblTip.Text = "发生错误，错误信息：" + e.Message;
                }));
                WriteLog(strDisplayName + " 试听发生错误，错误信息：" + e.Message);
            }

        }
        #endregion

        #region 功能函数
        /// <summary>
        /// 读取并写入配置
        /// </summary>
        private void ReadConfig()
        {
            string strText = "";
            int i = 0;
            if (File.Exists(m_strConfig))
            {
                StreamReader sr = new StreamReader(m_strConfig, Encoding.UTF8);
                while ((strText = sr.ReadLine()) != null)
                {
                    if ((strText.Length > 7) && (strText.Substring(0, 7) == "kgmid=="))
                    {
                        i = strText.IndexOf("++") + 2;
                        strText = strText.Substring(i, strText.IndexOf("--") - i);
                        clsMusicOperation.SetKgMid(strText);
                    }
                    if ((strText.Length > 8) && (strText.Substring(0, 8) == "kgdfid=="))
                    {
                        i = strText.IndexOf("++") + 2;
                        strText = strText.Substring(i, strText.IndexOf("--") - i);
                        clsMusicOperation.SetKgDfid(strText);
                    }
                }
                sr.Close();
            }
        }
        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="strText">写入内容</param>
        private void WriteLog(string strText)
        {
            strText = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + "  " + strText;

            if (File.Exists(m_strLog))
            {
                FileStream fs = new FileStream(m_strLog, FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(strText);
                sw.Flush();
                sw.Close();
            }
            else
            {
                FileStream fs = new FileStream(m_strLog, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(strText);
                sw.Flush();
                sw.Close();
            }
        }
        /// <summary>
        /// 设置lblTip的提示
        /// </summary>
        /// <param name="strText"></param>
        private void SetTipText(string strText)
        {
            lblTip.Text = strText;
        }
        
        /*
         *  向音乐列表中增加一项
         *  params:
         *        rowNum:当前行号
         *        music:clsMusic类型项
         *  return;none
         */
        private void AddAMusic(int rowNum, clsMusic music)
        {
            dataGVscan.Rows.Add();
            dataGVscan.Rows[rowNum].Cells[numbering].Value = rowNum + 1;
            dataGVscan.Rows[rowNum].Cells[name].Value = music.Name + (music.Subheading == "" ? "" : music.Subheading);
            dataGVscan.Rows[rowNum].Cells[singer].Value = music.Singer;
            dataGVscan.Rows[rowNum].Cells[album].Value = music.Class;
            dataGVscan.Rows[rowNum].Cells["dgvSource"].Value = music.Source;
            dataGVscan.Rows[rowNum].Cells["dgvName"].Value = music.Name;
            dataGVscan.Rows[rowNum].Cells["dgvSubheading"].Value = music.Subheading;
            if (music.CanDownload)
            {
                dataGVscan.Rows[rowNum].Cells["dgvCanDownload"].Value = "1";
            }
            else
            {
                dataGVscan.Rows[rowNum].Cells["dgvCanDownload"].Value = "0";
                dataGVscan.Rows[rowNum].DefaultCellStyle.BackColor = Color.Silver;
            }

            switch (music.Source)
            {
                case enmMusicSource.QQ:
                    dataGVscan.Rows[rowNum].Cells[5].Value = "QQ音乐";
                    dataGVscan.Rows[rowNum].Cells["dgvDownloadInfo"].Value = music.DownloadInfo;
                    break;
                case enmMusicSource.Kg:
                    dataGVscan.Rows[rowNum].Cells[5].Value = "酷狗音乐";
                    dataGVscan.Rows[rowNum].Cells["dgvDownloadInfo"].Value = music.DownloadInfo;
                    break;
                case enmMusicSource.Kw:
                    dataGVscan.Rows[rowNum].Cells[5].Value = "酷我音乐";
                    dataGVscan.Rows[rowNum].Cells["dgvDownloadInfo"].Value = music.DownloadInfo;
                    break;
                case enmMusicSource.Wyy:
                    dataGVscan.Rows[rowNum].Cells[5].Value = "网易云音乐";
                    dataGVscan.Rows[rowNum].Cells["dgvDownloadInfo"].Value = music.DownloadInfo;
                    break;
                default:
                    dataGVscan.Rows[rowNum].Cells[5].Value = "";
                    break;
            }
            //if (!lmsc[i].CanDownload) //隐藏不可下载的列
        }
        /// <summary>
        /// 判断歌曲是否能下载
        /// </summary>
        /// <param name="iRow">歌曲所在行</param>
        /// <returns></returns>
        private bool RowCanDownload(int iRow)
        {
            if (dataGVscan.Rows[iRow].Cells["dgvCanDownload"].Value.ToString() == "1")
                return true;
            else
                return false;
        }
        /// <summary>
        /// 返回要保存的文件名，未包括后缀格式
        /// </summary>
        /// <param name="iRow"></param>
        /// <returns></returns>
        private string GetFileName(int iRow)
        {
            string strResult = "";
            switch (m_intFormat)
            {
                case 0:
                    strResult = dataGVscan.Rows[iRow].Cells["dgvSinger"].Value.ToString() + " - " + dataGVscan.Rows[iRow].Cells["dgvName"].Value.ToString();
                    break;
                case 1:
                    strResult = dataGVscan.Rows[iRow].Cells["dgvSubheading"].Value.ToString();
                    strResult = dataGVscan.Rows[iRow].Cells["dgvName"].Value.ToString() + (strResult == "" ? "" : ("（" + strResult + "）"));
                    break;
                case 2:
                    strResult = dataGVscan.Rows[iRow].Cells["dgvSubheading"].Value.ToString();
                    strResult = dataGVscan.Rows[iRow].Cells["dgvName"].Value.ToString() + (strResult == "" ? "" : ("（" + strResult + "）")) + "[" + dataGVscan.Rows[iRow].Cells["dgvSinger"].Value.ToString() + "]";
                    break;
                case 3:
                    strResult = dataGVscan.Rows[iRow].Cells["dgvName"].Value.ToString() + "[" + dataGVscan.Rows[iRow].Cells["dgvSinger"].Value.ToString() + "]";
                    break;
                case 4:
                    strResult = dataGVscan.Rows[iRow].Cells["dgvName"].Value.ToString() + "(" + dataGVscan.Rows[iRow].Cells["dgvSinger"].Value.ToString() + ")";
                    break;
                case 6:
                    strResult = dataGVscan.Rows[iRow].Cells["dgvName"].Value.ToString() + " - " + dataGVscan.Rows[iRow].Cells["dgvSinger"].Value.ToString();
                    break;
                case 5:
                default:
                    strResult = dataGVscan.Rows[iRow].Cells["dgvName"].Value.ToString();
                    break;
            }
            //创建文件时提示：不支持给定路径的格式
            strResult = strResult.Replace("/", "_").Replace("\\", "_").Replace(":", "_").Replace("*", "_").Replace("?", "_").Replace("\"", "_").Replace("\'", "_").Replace("<", "_").Replace(">", "_").Replace("|", "_");

            return strResult;
        }
        /// <summary>
        /// 设置歌曲信息
        /// </summary>
        /// <param name="FilePath">文件路径</param>
        /// <param name="Title">标题</param>
        /// <param name="Author">作者</param>
        /// <param name="Description">描述</param>
        private void SetMusicInfo(string FilePath, string Title, string Author, string Description = "")
        {
            axWindowsMediaPlayer2.URL = "";
            axWindowsMediaPlayer2.URL = FilePath;
            axWindowsMediaPlayer2.Ctlcontrols.play();
            axWindowsMediaPlayer2.currentMedia.setItemInfo("Title", Title);
            axWindowsMediaPlayer2.currentMedia.setItemInfo("Author", Author);
            axWindowsMediaPlayer2.currentMedia.setItemInfo("Description", Description);
            axWindowsMediaPlayer2.URL = "";
        }

        #endregion
    }
}
