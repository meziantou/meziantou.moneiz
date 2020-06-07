using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Meziantou.Framework;

namespace Meziantou.Moneiz.Core
{
    partial class Database
    {
        [JsonIgnore]
        public IEnumerable<string> CategoryGroups => Categories.Select(c => c.GroupName).WhereNotNull().Distinct();

        public Category? GetCategoryById(int? id)
        {
            if (id == null)
                return null;

            return Categories.FirstOrDefault(item => item.Id == id);
        }


        public void RemoveCategory(Category category)
        {
            ReplaceCategory(oldCategory: category, newCategory: null);
            if (Categories.Remove(category))
            {
                RaiseDatabaseChanged();
            }
        }

        public void SaveCategory(Category category)
        {
            var existingCategory = Categories.FirstOrDefault(item => item.Id == category.Id);
            if (existingCategory == null)
            {
                category.Id = GenerateId(Categories, item => item.Id);
            }

            AddOrReplace(Categories, existingCategory, category);
            MergeCategories();
            RaiseDatabaseChanged();
        }

        private void MergeCategories()
        {
            foreach (var group in Categories.GroupBy(c => (c.GroupName, c.Name)))
            {
                var first = group.First();
                foreach (var c in group.Skip(1))
                {
                    ReplaceCategory(oldCategory: c, newCategory: first);
                    RemoveCategory(c);
                }
            }
        }

        private void ReplaceCategory(Category? oldCategory, Category? newCategory)
        {
            ReplaceCategory(newCategory, c => c == oldCategory);
        }

        private void ReplaceCategory(Category? newCategory, Func<Category?, bool> predicate)
        {
            // TODO Transaction, ScheduledTransaction, Preset, etc.
        }
    }
}
