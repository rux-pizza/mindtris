SHELL = /bin/sh
SYSTEM = $(shell uname)
C++ = g++
CC = gcc
DFLAGS =
OFLAGS = -O3
CFLAGS = $(OFLAGS) $(DFLAGS) -I. -I ../ -I ../Crypto++

CLIENTDIR = MindTris++
SERVERDIR = mt_server

SUBDIRS = $(CLIENTDIR) $(SERVERDIR)

ifeq ($(SYSTEM),Darwin)
DFLAGS += -D__APPLE__
OFLAGS += -flat_namespace
else
LFLAGS += -lrt
endif

ifeq ($(SYSTEM),FreeBSD)
DFLAGS += -D__FREEBSD__
endif

ifeq ($(SYSTEM),SunOS)
DFLAGS += -D__SOLARIS__
LFLAGS += -lresolv -lsocket -lnsl
endif

COREDIR=../mindtriscore


OBJS = database.o lobby.o mt_server.o user.o
COBJS =
PROGS =

all: $(OBJS) $(COBJS) $(PROGS)


clean:
	rm -f $(OBJS) $(COBJS) $(PROGS)

$(OBJS): %.o: %.cpp
	$(C++) -o $@ $(CFLAGS) -c $<

$(COBJS): %.o: %.c
	$(CC) -o $@ $(CFLAGS) -c $<



database.o: $(COREDIR)/includes.h database.h
lobby.o: $(COREDIR)/includes.h $(COREDIR)/oalloc.h $(COREDIR)/serverprotocol.h user.h lobby.h
mt_server.o: $(COREDIR)/includes.h $(COREDIR)/socket.h $(COREDIR)/oalloc.h $(COREDIR)/serverprotocol.h $(COREDIR)/p2pprotocol.h lobby.h user.h database.h mt_server.h
user.o: $(COREDIR)/includes.h $(COREDIR)/util.h $(COREDIR)/socket.h $(COREDIR)/oalloc.h $(COREDIR)/serverprotocol.h $(COREDIR)/p2pprotocol.h database.h mt_server.h lobby.h user.h
