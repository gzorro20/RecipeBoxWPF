﻿using RecipeBox3.SQLiteModel.Data;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace RecipeBox3.SQLiteModel.Adapters
{
    /// <summary>Adapter for Recipes table left joined with Categories, with optional image data</summary>
    public class DetailRecipesAdapter : RecipesBaseAdapter<DetailRecipe>
    {
        /// <summary>Adapter for images</summary>
        protected ImagesAdapter imagesAdapter = new ImagesAdapter();

        /// <inheritdoc/>
        protected override void Initialize(string connectionString)
        {
            base.Initialize(connectionString);

            // Override the auto command for the joined table
            SelectCommand.CommandText =
                String.Format(
                    "SELECT {0}, {1}, C_Name FROM {2} WHERE (@id IS NULL) OR ({0} = @id)",
                    IDColumnName,
                    String.Join(", ", DataColumnNames),
                    "Recipes LEFT JOIN Categories ON R_Category=C_ID");
        }
        
        /// <summary>Fetch a recipe by id including image data</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public DetailRecipe SelectWithImage(int id)
        {
            var recipe = base.Select(id);
            recipe.IMG_Data = imagesAdapter.SelectByRecipe(id)?.IMG_Data;
            return recipe;
        }

        /// <summary>Fetch all recipes in the database with image data</summary>
        /// <returns></returns>
        public IEnumerable<DetailRecipe> SelectAllWithImages()
        {
            List<DetailRecipe> recipeList = base.SelectAll().ToList();
            foreach (var recipe in recipeList)
            {
                recipe.IMG_Data = imagesAdapter.SelectByRecipe(recipe.ID)?.IMG_Data;
            }

            return recipeList;
        }

        /// <inheritdoc/>
        public override bool Insert(DetailRecipe row)
        {
            bool mainInsertSuccess = base.Insert(row);

            if (mainInsertSuccess)
            {
                int? recipeID = LastInsertedID;
                if (recipeID.HasValue)
                {
                    ImageRow imageRow = imagesAdapter.SelectByRecipe(recipeID.Value);

                    if (imageRow == null)
                    {
                        // doesn't exist yet
                        if (row.IMG_Data != null)
                        {
                            // new row
                            imageRow = new ImageRow()
                            {
                                IMG_RecipeID = recipeID,
                                IMG_Data = row.IMG_Data
                            };
                            imagesAdapter.Insert(imageRow);
                        }
                    }
                    else
                    {
                        // row exists
                        if (row.IMG_Data == null)
                        {
                            // delete
                            imagesAdapter.Delete(imageRow);
                        }
                        else
                        {
                            // update
                            imageRow.IMG_Data = row.IMG_Data;
                            imagesAdapter.Modify(imageRow);
                        }
                    }
                }
            }

            return mainInsertSuccess;
        }

        /// <inheritdoc/>
        public override bool Modify(DetailRecipe row)
        {
            ImageRow imageRow = imagesAdapter.SelectByRecipe(row.ID);

            if (imageRow == null && row.IMG_Data != null)
            {
                // create new row
                imageRow = new ImageRow()
                {
                    IMG_RecipeID = row.ID,
                    IMG_Data = row.IMG_Data
                };
            }
            else if (imageRow != null && row.IMG_Data == null)
            {
                // delete row from db
                imageRow.Status = RowStatus.Deleted;
            }
            else if (imageRow != null && row.IMG_Data != null)
            {
                // modify existing row
                imageRow.IMG_Data = row.IMG_Data;
            }

            if (imageRow != null) imagesAdapter.Update(imageRow);
            
            return base.Modify(row);
        }

        /// <inheritdoc/>
        public override bool Delete(DetailRecipe row)
        {
            ImageRow imageRow = imagesAdapter.SelectByRecipe(row.ID);
            imagesAdapter.Delete(imageRow);

            return base.Delete(row);
        }

        /// <inheritdoc/>
        protected override DetailRecipe GetRowFromReader(SQLiteDataReader reader)
        {
            try
            {
                var nextRow = new DetailRecipe
                {
                    R_ID = reader.GetInt32(0),
                    R_Name = reader.GetString(1),
                    R_Description = reader.GetString(2),
                    R_Modified = reader.GetNullableLong(3),
                    R_PrepTime = reader.GetInt32(4),
                    R_CookTime = reader.GetInt32(5),
                    R_Steps = reader.GetString(6),
                    R_Category = reader.GetInt32(7),
                    C_Name = reader.GetNullableString(8),
                    IMG_Data = null,
                    Status = RowStatus.Unchanged
                };

                return nextRow;
            }
            catch (InvalidCastException e)
            {
                App.LogException(e);
                return null;
            }
        }

        /// <inheritdoc/>
        protected override void SetDataParametersFromRow(DetailRecipe row)
        {
            base.SetDataParametersFromRow(row);
        }
    }
}
