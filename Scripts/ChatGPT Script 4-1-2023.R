# The purpose of this script is to process and analyze a series of CSV files containing accelerometer frequency data. 
# The script reads the files, filters the data based on frequency ranges, and creates four line plots for each file, representing different frequency bands. 
# The script then combines these four plots into a single image and saves the resulting image to a new directory with a high resolution (300 dpi). 
# The script is designed to handle multiple CSV files in a folder, process them one by one, and save the corresponding images automatically. 
# This allows for efficient analysis and visualization of accelerometer frequency data across multiple data files.

# This script and the above description were created by OpenAI's ChatGPT 4.0 model after a series of conversations based on my original Generate Graphs.r script from TestResults\September 24 Conversions\Converted
# It took some massaging, but it got there
# Only the file paths and resolution of the png images were directly customized by me, as well as the location of the base_path for easier future editing


library(dslabs)
library(tidyverse)
library(gridExtra)
library(GeneCycle)
library(spectral)

# Define the base path
base_path <- "C:/temp/September 24 Conversions/Converted/AccelerometerFreqAnalysisDuplicateIDs0/"

process_and_plot <- function(df, file_name, base_path, processed_dir) {
  titlex <- "Phase At Frequency"
  
  # Frequency ranges for different plots
  freq_ranges <- list(c(1, 1000), c(1001, 2000), c(2001, 3000), c(3001, 4000))
  plot_list <- list()
  
  for (i in seq_along(freq_ranges)) {
    range <- freq_ranges[[i]]
    plot_title <- paste("Frequency", range[1], "-", range[2])
    plot_df <- df %>% filter(X_Frequency >= range[1], X_Frequency < range[2])
    
    plot <- plot_df %>%
      ggplot(aes(x = X_Frequency, y = X_Magnitude)) +
      geom_line() +
      ggtitle(plot_title) +
      xlab("Frequency") +
      ylab("Magnitude")
    
    plot_list[[i]] <- plot
  }
  
  combined_plot <- arrangeGrob(grobs = plot_list, ncol = 1)
  ggsave(file.path(processed_dir, paste0(file_name, "_combined_plot.png")), combined_plot, width = 40, height = 28, dpi = 300)
}

# Create a new directory for processed files
processed_dir <- file.path(base_path, "Processed")
if (!dir.exists(processed_dir)) {
  dir.create(processed_dir)
}

# Read, process, and plot CSV files
file_paths <- list.files(
  path = base_path,
  pattern = "AccelerometerFreqAnalysisDuplicateIDs(\\d+).csv",
  full.names = T
)

for (file_path in file_paths) {
  file <- read.csv(file_path, header = TRUE, sep = ",")
  df <- file[, colSums(is.na(file)) < nrow(file)]
  
  # Extract the file number from the file name
  file_name <- gsub(".*(\\d+)\\.csv$", "AccelerometerFreqAnalysisDuplicateIDs\\1", basename(file_path))
  
  # Pass the base path and processed directory to the function
  process_and_plot(df, file_name, base_path, processed_dir)
}
