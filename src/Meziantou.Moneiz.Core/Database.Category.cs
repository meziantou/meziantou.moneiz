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
            using (DeferEvents())
            {
                ReplaceCategory(oldCategory: category, newCategory: null);
                Categories.Remove(category);
                RaiseDatabaseChanged();
            }
        }

        public void SaveCategory(Category category)
        {
            using (DeferEvents())
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
        }

        private void MergeCategories()
        {
            using (DeferEvents())
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
        }

        private void ReplaceCategory(Category? oldCategory, Category? newCategory)
        {
            ReplaceCategory(newCategory, c => c == oldCategory);
        }

        private void ReplaceCategory(Category? newCategory, Func<Category?, bool> predicate)
        {
            foreach (var payee in Payees)
            {
                if (predicate(payee.DefaultCategory))
                {
                    payee.DefaultCategory = newCategory;
                }
            }

            foreach (var scheduledTransactions in ScheduledTransactions)
            {
                if (predicate(scheduledTransactions.Category))
                {
                    scheduledTransactions.Category = newCategory;
                }
            }

            foreach (var transactions in Transactions)
            {
                if (predicate(transactions.Category))
                {
                    transactions.Category = newCategory;
                }
            }
        }
    }
}
