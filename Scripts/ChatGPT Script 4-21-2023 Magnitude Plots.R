library(tidyverse)
library(plotly)

# Define the base path
base_path <- "C:/GitHub/temp/Converted/"

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
  
  # Calculate the total amount of seconds
  total_seconds <- round(max(df$SECOND), 2)
  
  # Create the plotly plot
  plot <- plot_ly(df, x = ~ID, y = "X", z = ~X, type = "scatter3d", mode = "markers", name = "X", marker = list(size = 3, color = "blue")) %>%
    add_trace(x = ~ID, y = "Y", z = ~Y, type = "scatter3d", mode = "markers", name = "Y", marker = list(size = 3, color = "green")) %>%
    add_trace(x = ~ID, y = "Z", z = ~Z, type = "scatter3d", mode = "markers", name = "Z", marker = list(size = 3, color = "red")) %>%
    layout(
      title = paste("Accelerometer Data (Total time:", total_seconds, "seconds)"),
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
processed_dir <- file.path(base_path, "Processed")
if (!dir.exists(processed_dir)) {
  dir.create(processed_dir)
}

# Read, process, and plot CSV files
file_paths <- list.files(
  path = base_path,
  pattern = "Accelerometer(\\d+)\\.csv",
  full.names = T
)

for (file_path in file_paths) {
  file <- read.csv(file_path, header = TRUE, sep = ",")
  df <- file[, colSums(is.na(file)) < nrow(file)]
  
  # Extract the file number from the file name
  file_name <- gsub(".*(\\d+)\\.csv$", "Accelerometer\\1", basename(file_path))
  
  # Pass the base path and processed directory to the function
  process_and_plot(df, file_name, base_path, processed_dir)
}
