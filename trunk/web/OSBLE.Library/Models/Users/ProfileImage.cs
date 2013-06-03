using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace OSBLE.Models.Users
{
    public class ProfileImage : INotifyPropertyChanged, IModelBuilderExtender 
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        [Key]
        public int UserID { get; set; }

        [ForeignKey("UserID")]
        public virtual UserProfile UserProfile { get; set; }

        private byte[] _profileImage;
        [Column(TypeName = "image")]
        [DataMember]
        public byte[] Picture
        {
            get
            {
                return _profileImage;
            }
            set
            {
                _profileImage = value;
                OnPropertyChanged("ProfileImage");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public Bitmap GetProfileImage()
        {
            MemoryStream stream = new MemoryStream(Picture);
            Bitmap bmp = new Bitmap(stream);
            return bmp;
        }

        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProfileImage>()
                .HasRequired(p => p.UserProfile)
                .WithRequiredDependent(u => u.ProfileImage)
                .WillCascadeOnDelete(true);
        }
    }
}
