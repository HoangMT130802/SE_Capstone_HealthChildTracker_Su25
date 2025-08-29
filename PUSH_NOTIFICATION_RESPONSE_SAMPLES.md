# 📱 Mẫu Push Notification Response cho Frontend

## 🔔 **Cấu trúc chung của Firebase Message:**

```javascript
// Cấu trúc chung khi nhận push notification
{
  "messageId": "0:1640995200000000%abc123def456",
  "notification": {
    "title": "Tiêu đề thông báo",
    "body": "Nội dung thông báo"
  },
  "data": {
    "type": "loại_thông_báo",
    // ... các field khác tùy theo loại
  },
  "from": "123456789012",
  "collapseKey": "vaccine_reminder",
  "sentTime": 1640995200000,
  "ttl": 2419200
}
```

---

## 💉 **1. Vaccine Reminder (Nhắc nhở tiêm vaccine)**

### **📨 Response mẫu:**
```javascript
{
  "messageId": "0:1640995200000000%abc123def456",
  "notification": {
    "title": "🩺 Nhắc nhở tiêm vaccine",
    "body": "Bé Nguyễn Văn A sắp đến lịch tiêm Vaccine BCG mũi 1 vào ngày 15/01/2024 tại Bệnh viện Nhi Đồng 1"
  },
  "data": {
    "type": "vaccine_reminder",
    "childName": "Nguyễn Văn A",
    "vaccineName": "Vaccine BCG",
    "doseNumber": "1",
    "expectedDate": "15/01/2024",
    "facilityName": "Bệnh viện Nhi Đồng 1"
  },
  "from": "childtrack-eae9b",
  "collapseKey": "vaccine_reminder",
  "sentTime": 1640995200000,
  "ttl": 2419200
}
```

### **🎯 Frontend xử lý:**
```javascript
// React Native / Expo
import messaging from '@react-native-firebase/messaging';

// App đang foreground
messaging().onMessage(async remoteMessage => {
  if (remoteMessage.data.type === 'vaccine_reminder') {
    const { childName, vaccineName, doseNumber, expectedDate, facilityName } = remoteMessage.data;
    
    // Hiển thị in-app notification
    showInAppNotification({
      title: remoteMessage.notification.title,
      body: remoteMessage.notification.body,
      icon: '💉',
      action: () => {
        // Navigate đến màn hình vaccine schedule
        navigation.navigate('VaccineSchedule', {
          childName: childName,
          highlightVaccine: vaccineName,
          doseNumber: parseInt(doseNumber)
        });
      }
    });
    
    // Update badge counter
    updateVaccineReminderCount(+1);
  }
});

// User tap notification khi app background
messaging().onNotificationOpenedApp(remoteMessage => {
  if (remoteMessage.data.type === 'vaccine_reminder') {
    const { childName, vaccineName, doseNumber } = remoteMessage.data;
    
    // Direct navigation
    navigation.navigate('VaccineSchedule', {
      childName: childName,
      highlightVaccine: vaccineName,
      doseNumber: parseInt(doseNumber)
    });
  }
});
```

---

## 📅 **2. Appointment Reminder (Nhắc nhở lịch hẹn)**

### **📨 Response mẫu:**
```javascript
{
  "messageId": "0:1640995300000000%def456ghi789",
  "notification": {
    "title": "📅 Nhắc nhở lịch hẹn",
    "body": "Bé Trần Thị B có lịch hẹn vào 10:30 ngày 16/01/2024 tại Phòng khám Đa khoa ABC"
  },
  "data": {
    "type": "appointment_reminder",
    "childName": "Trần Thị B",
    "appointmentDate": "16/01/2024",
    "appointmentTime": "10:30",
    "facilityName": "Phòng khám Đa khoa ABC",
    "facilityAddress": "123 Nguyễn Văn Linh, Quận 7, TP.HCM"
  },
  "from": "childtrack-eae9b",
  "collapseKey": "appointment_reminder",
  "sentTime": 1640995300000,
  "ttl": 2419200
}
```

### **🎯 Frontend xử lý:**
```javascript
// App đang foreground
messaging().onMessage(async remoteMessage => {
  if (remoteMessage.data.type === 'appointment_reminder') {
    const { 
      childName, 
      appointmentDate, 
      appointmentTime, 
      facilityName, 
      facilityAddress 
    } = remoteMessage.data;
    
    // Hiển thị in-app notification với action buttons
    showInAppNotification({
      title: remoteMessage.notification.title,
      body: remoteMessage.notification.body,
      icon: '📅',
      actions: [
        {
          text: 'Xem chi tiết',
          action: () => {
            navigation.navigate('AppointmentDetail', {
              childName: childName,
              date: appointmentDate,
              time: appointmentTime,
              facility: facilityName,
              address: facilityAddress
            });
          }
        },
        {
          text: 'Đặt nhắc nhở',
          action: () => {
            // Set local reminder 30 minutes before
            setLocalReminder({
              title: `Lịch hẹn của ${childName}`,
              body: `Còn 30 phút nữa đến lịch hẹn tại ${facilityName}`,
              triggerTime: calculateReminderTime(appointmentDate, appointmentTime, -30)
            });
          }
        }
      ]
    });
    
    // Update appointment badge
    updateAppointmentReminderCount(+1);
  }
});
```

---

## ✅ **3. Vaccination Completion (Hoàn thành tiêm)**

### **📨 Response mẫu:**
```javascript
{
  "messageId": "0:1640995400000000%ghi789jkl012",
  "notification": {
    "title": "✅ Tiêm vaccine thành công",
    "body": "Bé Lê Văn C đã hoàn thành tiêm Vaccine Bại liệt mũi 2 vào ngày 17/01/2024 tại Trung tâm Y tế Quận 1"
  },
  "data": {
    "type": "vaccination_completion",
    "childName": "Lê Văn C",
    "vaccineName": "Vaccine Bại liệt",
    "doseNumber": "2",
    "completionDate": "17/01/2024",
    "facilityName": "Trung tâm Y tế Quận 1"
  },
  "from": "childtrack-eae9b",
  "collapseKey": "vaccination_completion",
  "sentTime": 1640995400000,
  "ttl": 2419200
}
```

### **🎯 Frontend xử lý:**
```javascript
// App đang foreground
messaging().onMessage(async remoteMessage => {
  if (remoteMessage.data.type === 'vaccination_completion') {
    const { 
      childName, 
      vaccineName, 
      doseNumber, 
      completionDate, 
      facilityName 
    } = remoteMessage.data;
    
    // Hiển thị success notification
    showSuccessNotification({
      title: remoteMessage.notification.title,
      body: remoteMessage.notification.body,
      icon: '🎉',
      duration: 5000, // Show longer for success
      action: () => {
        navigation.navigate('VaccinationRecord', {
          childName: childName,
          highlightRecord: {
            vaccine: vaccineName,
            dose: parseInt(doseNumber),
            date: completionDate
          }
        });
      }
    });
    
    // Update local vaccine record
    updateLocalVaccineRecord({
      childName: childName,
      vaccine: vaccineName,
      dose: parseInt(doseNumber),
      completedDate: completionDate,
      facility: facilityName,
      status: 'completed'
    });
    
    // Clear any pending reminders for this vaccine
    clearVaccineReminder(childName, vaccineName, doseNumber);
  }
});
```

---

## 🔧 **4. Utility Functions cho Frontend:**

### **📱 Push Notification Handler:**
```javascript
// utils/pushNotificationHandler.js
export const handlePushNotification = (remoteMessage) => {
  const { type } = remoteMessage.data;
  
  switch (type) {
    case 'vaccine_reminder':
      return handleVaccineReminder(remoteMessage);
    
    case 'appointment_reminder':
      return handleAppointmentReminder(remoteMessage);
    
    case 'vaccination_completion':
      return handleVaccinationCompletion(remoteMessage);
    
    default:
      console.log('Unknown notification type:', type);
      return null;
  }
};

const handleVaccineReminder = (message) => {
  const { childName, vaccineName, doseNumber, expectedDate } = message.data;
  
  return {
    type: 'vaccine_reminder',
    title: message.notification.title,
    body: message.notification.body,
    icon: '💉',
    color: '#FF6B6B',
    route: 'VaccineSchedule',
    params: { childName, vaccineName, doseNumber: parseInt(doseNumber) },
    priority: 'high'
  };
};

const handleAppointmentReminder = (message) => {
  const { childName, appointmentDate, appointmentTime } = message.data;
  
  return {
    type: 'appointment_reminder',
    title: message.notification.title,
    body: message.notification.body,
    icon: '📅',
    color: '#4ECDC4',
    route: 'AppointmentDetail',
    params: { childName, date: appointmentDate, time: appointmentTime },
    priority: 'high'
  };
};

const handleVaccinationCompletion = (message) => {
  const { childName, vaccineName, doseNumber } = message.data;
  
  return {
    type: 'vaccination_completion',
    title: message.notification.title,
    body: message.notification.body,
    icon: '✅',
    color: '#95E1D3',
    route: 'VaccinationRecord',
    params: { childName, vaccine: vaccineName, dose: parseInt(doseNumber) },
    priority: 'normal'
  };
};
```

### **🎨 In-App Notification Component:**
```javascript
// components/InAppNotification.jsx
import React from 'react';
import { View, Text, TouchableOpacity, Animated } from 'react-native';

const InAppNotification = ({ notification, onPress, onDismiss }) => {
  return (
    <Animated.View style={[styles.container, { backgroundColor: notification.color }]}>
      <TouchableOpacity onPress={onPress} style={styles.content}>
        <Text style={styles.icon}>{notification.icon}</Text>
        <View style={styles.textContainer}>
          <Text style={styles.title}>{notification.title}</Text>
          <Text style={styles.body}>{notification.body}</Text>
        </View>
      </TouchableOpacity>
      <TouchableOpacity onPress={onDismiss} style={styles.closeButton}>
        <Text style={styles.closeText}>×</Text>
      </TouchableOpacity>
    </Animated.View>
  );
};

const styles = {
  container: {
    position: 'absolute',
    top: 50,
    left: 20,
    right: 20,
    borderRadius: 12,
    padding: 16,
    flexDirection: 'row',
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.25,
    shadowRadius: 3.84,
    elevation: 5,
    zIndex: 1000
  },
  content: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center'
  },
  icon: {
    fontSize: 24,
    marginRight: 12
  },
  textContainer: {
    flex: 1
  },
  title: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#fff',
    marginBottom: 4
  },
  body: {
    fontSize: 14,
    color: '#fff',
    opacity: 0.9
  },
  closeButton: {
    padding: 4
  },
  closeText: {
    fontSize: 20,
    color: '#fff',
    fontWeight: 'bold'
  }
};
```

### **📊 Notification State Management:**
```javascript
// store/notificationSlice.js (Redux Toolkit)
import { createSlice } from '@reduxjs/toolkit';

const notificationSlice = createSlice({
  name: 'notifications',
  initialState: {
    vaccineReminders: 0,
    appointmentReminders: 0,
    unreadCount: 0,
    history: []
  },
  reducers: {
    addNotification: (state, action) => {
      const notification = action.payload;
      state.history.unshift(notification);
      state.unreadCount += 1;
      
      if (notification.type === 'vaccine_reminder') {
        state.vaccineReminders += 1;
      } else if (notification.type === 'appointment_reminder') {
        state.appointmentReminders += 1;
      }
    },
    markAsRead: (state, action) => {
      const notificationId = action.payload;
      const notification = state.history.find(n => n.id === notificationId);
      if (notification && !notification.read) {
        notification.read = true;
        state.unreadCount -= 1;
      }
    },
    clearVaccineReminders: (state) => {
      state.vaccineReminders = 0;
    },
    clearAppointmentReminders: (state) => {
      state.appointmentReminders = 0;
    }
  }
});

export const { 
  addNotification, 
  markAsRead, 
  clearVaccineReminders, 
  clearAppointmentReminders 
} = notificationSlice.actions;
export default notificationSlice.reducer;
```

---

## 🎯 **Tóm tắt cho Frontend Developer:**

### **📋 Checklist Implementation:**

- [ ] Setup Firebase messaging in app
- [ ] Handle 3 loại notification: `vaccine_reminder`, `appointment_reminder`, `vaccination_completion`  
- [ ] Implement navigation routing based on `data.type`
- [ ] Create in-app notification component
- [ ] Add badge counters for unread notifications
- [ ] Store notification history in local state/storage
- [ ] Handle foreground, background, và terminated app states
- [ ] Add local reminder scheduling
- [ ] Implement notification sound/vibration
- [ ] Test trên cả Android và iOS

### **🔔 Key Data Fields để xử lý:**

| Field | Purpose | Example |
|-------|---------|---------|
| `type` | Route logic | `"vaccine_reminder"` |
| `childName` | Display & filter | `"Nguyễn Văn A"` |
| `vaccineName` | Highlight vaccine | `"Vaccine BCG"` |
| `doseNumber` | Show progress | `"1"` |
| `expectedDate` | Schedule info | `"15/01/2024"` |
| `facilityName` | Location info | `"Bệnh viện Nhi Đồng"` |

**Ready để implement push notifications! 🚀📱**
