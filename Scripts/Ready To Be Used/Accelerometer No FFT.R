#This R code processes the output of the `ConvertAccelDataNoAnalysis`
#function. The code reads CSV files containing accelerometer data, 
#including X, Y, and Z axes, the unique ID, and the timestamp. 

#It then creates a 3D scatter plot of the accelerometer data for 
#each axis and saves it as an HTML file. 
#The `ConvertAccelDataNoAnalysis` function generates CSV files 
#containing raw and processed accelerometer data for X, Y, and Z axes,
#along with the timestamp and CPU temperature for each record, 
#which matches the data being used in the R code.

library(tidyverse)
library(plotly)

# Detect the script's directory
if (interactive()) {
  # If running the script in RStudio
  script_path <- rstudioapi::getSourceEditorContext()$path
} else {
  # If running the script from the command line
  script_path <- commandArgs(trailingOnly = TRUE)[1]
}
script_dir <- dirname(tools::file_path_as_absolute(script_path))

# Define the base path
base_path <- file.path(script_dir, "Converted")

process_and_plot <- function(df, file_name, base_path, processed_dir) {
  # Convert data types
  df <- df %>%
    mutate(
      ID = as.integer(ID),
      SECOND = as.numeric(SECOND),
      X = as.numeric(X),
      Y = as.numeric(Y),
      Z = as.numeric(Z)
    )
  print(base_path)
  print(file_name)
  
  # Calculate the total amount of seconds
  max <- max(df$SECOND, na.rm = TRUE)
  min <- min(df$SECOND, na.rm = TRUE)
  
  # Create the plotly plot
  plot <- plot_ly(df, x = ~ID, y = "X", z = ~X, type = "scatter3d", mode = "markers", name = "X", marker = list(size = 3, color = "blue")) %>%
    add_trace(x = ~ID, y = "Y", z = ~Y, type = "scatter3d", mode = "markers", name = "Y", marker = list(size = 3, color = "green")) %>%
    add_trace(x = ~ID, y = "Z", z = ~Z, type = "scatter3d", mode = "markers", name = "Z", marker = list(size = 3, color = "red")) %>%
    layout(
      title = paste("Accelerometer Data (Total time:", min, "-", max, "seconds)"),
      scene = list(
        xaxis = list(title = "Unique ID"),
        yaxis = list(title = "Accelerometer Axis"),
        zaxis = list(title = "Acceleration (0-5 Volts)")
      )
    )
  
  # Save the plotly plot as an HTML file
  htmlwidgets::saveWidget(plot, file.path(processed_dir, paste0(file_name, "_acceleration_data_3d_plot.html")))
}

# Create a new directory for processed files
processed_dir <- file.path(base_path, "Processed/Accelerometer")
if (!dir.exists(processed_dir)) {
  dir.create(processed_dir, recursive = TRUE)
}

# Read, process, and plot CSV files
file_paths <- list.files(
  path = base_path,
  pattern = "Accelerometer(\\d+)\\.csv",
  full.names = TRUE
)

for (file_path in file_paths) {
  file <- read.csv(file_path, header = TRUE, sep = ",")
  df <- file[, colSums(is.na(file)) < nrow(file)]
  
  # Extract the file number from the file name
  file_name <- gsub("^Accelerometer(\\d+)\\.csv$", "Accelerometer\\1", basename(file_path))
  
  # Pass the base path and processed directory to the function
  process_and_plot(df, file_name, base_path, processed_dir)
}
