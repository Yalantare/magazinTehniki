using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TehnikiApp.Models
{
    public partial class Product
    {
        [Key]
        public int Articul { get; set; }

        public string Title { get; set; } = null!;

        public string Manufacturer { get; set; } = null!;

        public int Category { get; set; }

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public string? Description { get; set; }

        public string Photo { get; set; } = null!;


        public virtual Categorye? CategoryNavigation { get; set; } = null!;

        public virtual ICollection<ReceiptItem> ReceiptItems { get; set; } = new List<ReceiptItem>();

        public virtual ICollection<ProductVariation> ProductVariations { get; set; } = new List<ProductVariation>();


        [NotMapped]
        public string FullPhotoPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Photo))
                {
                    try
                    {
                        return new Uri("C:\\Users\\User\\source\\repos\\TehnikiApp\\TehnikiApp\\Resourses\\default.png").AbsoluteUri;
                    }
                    catch
                    {
                        return "C:\\Users\\User\\source\\repos\\TehnikiApp\\TehnikiApp\\Resourses\\default.png";
                    }
                }

                string rawName = Photo.Trim();

                
                if (System.IO.File.Exists(rawName))
                {
                    try
                    {
                        return new Uri(rawName).AbsoluteUri;
                    }
                    catch
                    {
                        return rawName;
                    }
                }

                
                string fileName = rawName;
                
                int resoursesIndex = rawName.IndexOf("Resourses", StringComparison.OrdinalIgnoreCase);
                if (resoursesIndex >= 0)
                {
                    fileName = rawName.Substring(resoursesIndex + "Resourses".Length).TrimStart('\\', '/');
                }
                else
                {
                    int resourcesIndex = rawName.IndexOf("Resources", StringComparison.OrdinalIgnoreCase);
                    if (resourcesIndex >= 0)
                    {
                        fileName = rawName.Substring(resourcesIndex + "Resources".Length).TrimStart('\\', '/');
                    }
                    else
                    {
                        try
                        {
                            fileName = System.IO.Path.GetFileName(rawName);
                        }
                        catch
                        {
                            fileName = rawName;
                        }
                    }
                }

                
                string localResoursesPath = System.IO.Path.Combine("C:\\Users\\User\\source\\repos\\TehnikiApp\\TehnikiApp\\Resourses", fileName);
                if (System.IO.File.Exists(localResoursesPath))
                {
                    try
                    {
                        return new Uri(localResoursesPath).AbsoluteUri;
                    }
                    catch
                    {
                        return localResoursesPath;
                    }
                }

                
                if (fileName.Equals("default.png", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        return new Uri("C:\\Users\\User\\source\\repos\\TehnikiApp\\TehnikiApp\\Resourses\\default.png").AbsoluteUri;
                    }
                    catch
                    {
                        return "C:\\Users\\User\\source\\repos\\TehnikiApp\\TehnikiApp\\Resourses\\default.png";
                    }
                }

                
                return $"http://localhost:8090/images/{fileName}";
            }
        }

        [NotMapped]
        public string AvailabilityText => Stock > 0 ? "В наличии" : "Нет в наличии";

        [NotMapped]
        public string AvailabilityColor => Stock > 0 ? "#10B981" : "#EF4444";

        [NotMapped]
        public bool IsAvailable => Stock > 0;
    }
}
