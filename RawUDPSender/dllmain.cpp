// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include "pch.h"

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

#ifdef WIN32
#include <winsock2.h>
#include <windows.h>
#include <WS2tcpip.h>
#include "protoinfo.h"
#pragma comment (lib,"ws2_32.lib")

USHORT CheckSum(PUSHORT buf, int size)
{
    ULONG  sum = 0;
    while (size > 1)
    {
        sum += *buf;
        buf++;
        size -= 2;
    }
    if (size)
        sum += *(char*)buf;
    sum = (sum >> 16) + (sum & 0xffff);
    sum += sum >> 16;
    return (~sum);

}
using UDPFakeHead = struct
{
	ULONG sourceIp;             /*IP*/
	ULONG destIp;             /*IP*/
	UINT8 filling;                                 /*0*/
	UINT8 protocalType;                             /*IP*/
	UINT16 UDPLength;                               /*UDP*/
};
static int Win32SendUDPRequest(char* srcIp, int srcPort, char* dstIp, int dstPort, unsigned char* buffer, int buffersize) {
    int  msgLen = buffersize;
    int ret;
    WSADATA wsaData;
    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
    {
        return -1;
    }
    SOCKET sockRaw = socket(AF_INET, SOCK_RAW, IPPROTO_RAW);
	int h_incl = 1;
	ret = setsockopt(sockRaw, IPPROTO_IP, IP_HDRINCL, (char*)&h_incl, sizeof(h_incl));
	//创建并填充IP首部
	char buf[1024] = { 0 };
	IPHeader* pIpHead = (IPHeader*)buf;
	pIpHead->iphVerLen = 4 << 4 | (sizeof(IPHeader) / sizeof(ULONG));
	pIpHead->ipLength = htons(sizeof(IPHeader) + sizeof(UDPHeader));
	pIpHead->ipTTL = 128;
	pIpHead->ipProtocol = IPPROTO_UDP;
	pIpHead->ipSource = inet_addr(srcIp);
	pIpHead->ipDestination = inet_addr(dstIp);
	pIpHead->ipChecksum = CheckSum((PUSHORT)pIpHead, sizeof(IPHeader));//IP 首部的校验和只需要校验IP首部
	//填充UDP首部
	UDPHeader* pUdpHeader = (UDPHeader*)&buf[sizeof(IPHeader)];
	pUdpHeader->sourcePort = htons(srcPort);
	pUdpHeader->destinationPort = htons(dstPort);
	pUdpHeader->len = htons(sizeof(UDPHeader) + buffersize);
	pUdpHeader->checksum = 0;
	//填充UDP数据
	char* pData;
	pData = &buf[sizeof(IPHeader) + sizeof(UDPHeader)];
	memcpy(pData, buffer, buffersize);
	//计算UDP校验和
	UDPFakeHead mUDPFakeHead;
	mUDPFakeHead.sourceIp = pIpHead->ipSource;
	mUDPFakeHead.destIp = pIpHead->ipDestination;
	mUDPFakeHead.filling = 0;
	mUDPFakeHead.protocalType = IPPROTO_UDP;
	mUDPFakeHead.UDPLength = htons(sizeof(UDPHeader) + buffersize);

	//设置szBuffer目的是计算UDP校验和
	char szBuffer[1024];
	memcpy(szBuffer, &mUDPFakeHead, sizeof(UDPFakeHead));
	memcpy(&szBuffer[sizeof(UDPFakeHead)], pUdpHeader, sizeof(UDPHeader));
	memcpy(&szBuffer[sizeof(UDPFakeHead) + sizeof(UDPHeader)], buffer, buffersize);
	pUdpHeader->checksum = CheckSum((PUSHORT)szBuffer, sizeof(UDPFakeHead) + sizeof(UDPHeader) + buffersize);
	//设置目的地址
	sockaddr_in addrDest;
	addrDest.sin_family = AF_INET;
	addrDest.sin_addr.S_un.S_addr = inet_addr(dstIp);
	addrDest.sin_port = htons(dstPort);
	ret = sendto(sockRaw, buf, sizeof(UDPFakeHead) + sizeof(UDPHeader) + buffersize, 0, (sockaddr*)&addrDest, sizeof(addrDest));
	if (ret == SOCKET_ERROR)
		return -1;
	closesocket(sockRaw);
	WSACleanup();
	return ret;
}
#endif


extern "C" __declspec(dllexport) int SendUDPRequest(char* srcIp, int srcPort, char* dstIp, int dstPort, unsigned char* buffer, int buffersize) {
#ifdef WIN32
	return Win32SendUDPRequest(srcIp, srcPort, dstIp, dstPort, buffer, buffersize);
#endif // WIN32
#ifdef __linux__
	return LinuxSendUDPRequest(srcIp, srcPort, dstIp, dstPort, buffer, buffersize);
#endif // __linux__
	//no fuck jobs shit
}