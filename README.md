# VirtualDriveApp

You need to create a library that implements access to the virtual file system, physically storing all the data in one file. The library must maintain a hierarchy of directories, long file names. For each file, you need to store the name, content, creation time, and modification time. The library should allow the following operations:\
 \
Creating a folder\
File creation\
Delete file / folder\
Move / rename file / folder\
Copy file / folder\
Search for files by mask in this directory (including recursively). The API for searching files should provide an iterator, and not return a generated list.\
Reading / writing to a file (sequentially and randomly, and changing the file size) Importing the contents of a folder from a real file system or another similar virtual system. The library should allow multi-threaded access to the file system with read / write blocking. If a file within the system is open for writing, the system should prohibit any parallel access to the contents of the file. If the file is open for reading, the system should allow parallel reading, but prohibit any modification operations all the way to the root.\
A window user interface should be supplied to the library that allows you to open a certain instance of the virtual file system (or create a new one) and perform all the specified operations in it. Long-term operations should be performed in a separate thread and not block further interaction with the system, including and launching other operations. The appearance of the program can be arbitrary, for example, a tree of folders in one panel and the contents of a folder in another.\
