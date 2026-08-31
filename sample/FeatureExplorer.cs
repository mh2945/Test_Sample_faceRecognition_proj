using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FaceSDKSample.sample
{
    internal class FeatureExplorer
    {
        public static bool show_dlg(IWin32Window owner, string startup_path)
        {            
            using (var dlg = new FeatureExplorerDlg("FeatureExplorer", startup_path, startup_path))
            {
                if (dlg.ShowDialog(owner) == DialogResult.OK)
                {
                    // confirmed
                }
            }

            return true;
        }
    }
}
