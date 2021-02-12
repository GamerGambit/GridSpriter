using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace GridSpriter
{
	public static class Extensions
	{
        // Courtesy of https://stackoverflow.com/a/1759923
        /// <summary>
        /// Finds a Child of a given item in the visual tree. 
        /// </summary>
        /// <param name="parent">A direct parent of the queried item.</param>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="childName">x:Name or Name of child. </param>
        /// <returns>The first parent item that matches the submitted type parameter. 
        /// If not matching item can be found, 
        /// a null parent is being returned.</returns>
        public static T FindChild<T>(this DependencyObject parent, string childName)
           where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
				// If the child is not of the request child type child
				if (!(child is T t))
				{
					// recursively drill down the tree
					foundChild = FindChild<T>(child, childName);

					// If the child is found, break so we do not overwrite the found child. 
					if (foundChild != null) break;
				}
				else if (!string.IsNullOrEmpty(childName))
				{
					// If the child's name is set for search
					if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
					{
						// if the child's name is of the request name
						foundChild = t;
						break;
					}
				}
				else
				{
					// child element found.
					foundChild = t;
					break;
				}
			}

            return foundChild;
        }
    }
}
