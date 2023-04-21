library(tidyverse)
library(plotly)

# Define the base path
base_path <- "C:/GitHub/temp/Converted/"

process_and_plot <- function(df, file_name, base_path, processed_dir) {
  # Convert data types
  df <- df %>%
    mutate(
      SECOND = as.numeric(SECOND),
      X = as.numeric(X),
      Y = as.numeric(Y),
      Z = as.numeric(Z)
    )
  
  # Calculate the acceleration magnitude
  df <- df %>%
    mutate(
      MAGNITUDE = sqrt(X^2 + Y^2 + Z^2)
    )
  
  # 1. Plot the acceleration magnitude over time
  plot_magnitude <- df %>%
    ggplot(aes(x = SECOND, y = MAGNITUDE)) +
    geom_line() +
    ggtitle("Acceleration Magnitude over Time") +
    xlab("Time (Seconds)") +
    ylab("Acceleration Magnitude")
  
  ggsave(file.path(processed_dir, paste0(file_name, "_acceleration_magnitude_plot.png")), plot_magnitude, width = 12, height = 8, dpi = 300)
  
  # 2. Create a 3D scatter plot of the accelerometer data
  plot_3d <- plot_ly(df, x = ~X, y = ~Y, z = ~Z, type = "scatter3d", mode = "markers", marker = list(size = 1)) %>%
    layout(scene = list(xaxis = list(title = "X-axis"), yaxis = list(title = "Y-axis"), zaxis = list(title = "Z-axis")))
  
  # Save the 3D scatter plot as an HTML file
  htmlwidgets::saveWidget(plot_3d, file.path(processed_dir, paste0(file_name, "_acceleration_3d_plot.html")))
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
