library(dslabs)
library(tidyverse)
library(gridExtra)
library(GeneCycle)
library(spectral)
file <- read.csv("C:/Users/Walte/Desktop/September 24 Conversions/Converted/AccelerometerFreqAnalysisDuplicateIDs0.csv", header = TRUE, sep=",")

head(file)
df <- file[,colSums(is.na(file))<nrow(file)]

titlex <- "Phase At Frequency"
#df %>% filter(Frequency < 31, Frequency >= 26) %>%
#ggplot(aes(y = Phase,x = Frequency )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Phase")

#df %>% filter(Frequency < 31, Frequency >= 26) %>%
#  ggplot(aes(x = Magnitude,y = Frequency )) + geom_line() + ggtitle(titlex) + xlab("Magnitude") +ylab("Frequency")


#Specific second data
titlex <- "Frequency 1 - 1000"
P_1 <- df %>% filter(X_Frequency < 1000, X_Frequency >= 1, Second == 2) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")

titlex <- "Frequency 1001 - 2000"
P_2 <- df %>% filter(X_Frequency < 2000, X_Frequency >= 1001, Second == 2) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")


titlex <- "Frequency 2001 - 3000"
P_3 <- df %>% filter(X_Frequency < 3000, X_Frequency >= 2001, Second == 2) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")

titlex <- "Frequency 3001 - 4000"
P_4 <- df %>% filter(X_Frequency < 4000, X_Frequency >= 3001, Second == 2) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")




grid.arrange(P_1, P_2, P_3, P_4, ncol= 1)


#All seconds data
titlex <- "Frequency 1 - 1000"
P_1 <- df %>% filter(X_Frequency < 1000, X_Frequency >= 1) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")

titlex <- "Frequency 1001 - 2000"
P_2 <- df %>% filter(X_Frequency < 2000, X_Frequency >= 1001) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")


titlex <- "Frequency 2001 - 3000"
P_3 <- df %>% filter(X_Frequency < 3000, X_Frequency >= 2001) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")

titlex <- "Frequency 3001 - 4000"
P_4 <- df %>% filter(X_Frequency < 4000, X_Frequency >= 3001) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")




grid.arrange(P_1, P_2, P_3, P_4, ncol= 1)



#second file
file <- read.csv("C:/Users/Walte/Desktop/September 24 Conversions/Converted/AccelerometerFreqAnalysisDuplicateIDs1.csv", header = TRUE, sep=",")

head(file)
df <- file[,colSums(is.na(file))<nrow(file)]

#All seconds data
titlex <- "Frequency 1 - 1000"
P_1 <- df %>% filter(X_Frequency < 1000, X_Frequency >= 1) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")

titlex <- "Frequency 1001 - 2000"
P_2 <- df %>% filter(X_Frequency < 2000, X_Frequency >= 1001) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")


titlex <- "Frequency 2001 - 3000"
P_3 <- df %>% filter(X_Frequency < 3000, X_Frequency >= 2001) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")

titlex <- "Frequency 3001 - 4000"
P_4 <- df %>% filter(X_Frequency < 4000, X_Frequency >= 3001) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")




grid.arrange(P_1, P_2, P_3, P_4, ncol= 1)

#Third file
file <- read.csv("C:/Users/Walte/Desktop/September 24 Conversions/Converted/AccelerometerFreqAnalysisDuplicateIDs2.csv", header = TRUE, sep=",")

head(file)
df <- file[,colSums(is.na(file))<nrow(file)]

#All seconds data
titlex <- "Frequency 1 - 1000"
P_1 <- df %>% filter(X_Frequency < 1000, X_Frequency >= 1) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")

titlex <- "Frequency 1001 - 2000"
P_2 <- df %>% filter(X_Frequency < 2000, X_Frequency >= 1001) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")


titlex <- "Frequency 2001 - 3000"
P_3 <- df %>% filter(X_Frequency < 3000, X_Frequency >= 2001) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")

titlex <- "Frequency 3001 - 4000"
P_4 <- df %>% filter(X_Frequency < 4000, X_Frequency >= 3001) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")




grid.arrange(P_1, P_2, P_3, P_4, ncol= 1)


#Fourth file
file <- read.csv("C:/Users/Walte/Desktop/September 24 Conversions/Converted/AccelerometerFreqAnalysisDuplicateIDs3.csv", header = TRUE, sep=",")

head(file)
df <- file[,colSums(is.na(file))<nrow(file)]

#All seconds data
titlex <- "Frequency 1 - 1000"
P_1 <- df %>% filter(X_Frequency < 1000, X_Frequency >= 1) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")

titlex <- "Frequency 1001 - 2000"
P_2 <- df %>% filter(X_Frequency < 2000, X_Frequency >= 1001) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")


titlex <- "Frequency 2001 - 3000"
P_3 <- df %>% filter(X_Frequency < 3000, X_Frequency >= 2001) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")

titlex <- "Frequency 3001 - 4000"
P_4 <- df %>% filter(X_Frequency < 4000, X_Frequency >= 3001) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")




grid.arrange(P_1, P_2, P_3, P_4, ncol= 1)







#All files at once

tbl <-
  list.files(path = "C:/Users/Walte/Desktop/September 24 Conversions/Converted/",
             pattern = ".*IDs(\\d+).csv", 
             full.names = T) %>% 
  map_df(~read_csv(., col_types = cols(.default = "c"))) 




titlex <- "Frequency 2001 - 3000"
P_3 <- tbl %>% filter(X_Frequency < 2500, X_Frequency >= 2001) %>%
  ggplot(aes(x = X_Frequency,y = X_Magnitude )) + geom_line() + ggtitle(titlex) + xlab("Frequency") +ylab("Magnitude")


grid.arrange(P_3, ncol= 1)
max(tbl$X_Magnitude)
